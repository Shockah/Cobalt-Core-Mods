using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	public IKokoroApi.IActionCostApi ActionCosts { get; } = new ActionCostApiImplementation();
	
	public sealed class ActionCostApiImplementation : IKokoroApi.IActionCostApi
	{
		public CardAction Make(IKokoroApi.IActionCostApi.IActionCost cost, CardAction action)
			=> new AResourceCost { Cost = MakeCostFromLegacyData(cost), Action = action };

		public CardAction Make(IReadOnlyList<IKokoroApi.IActionCostApi.IActionCost> costs, CardAction action)
			=> new AResourceCost { Cost = new CombinedResourceActionCost { Costs = costs.Select(MakeCostFromLegacyData).ToList() }, Action = action };

		public IKokoroApi.IActionCostApi.IActionCost Cost(IReadOnlyList<IKokoroApi.IActionCostApi.IResource> potentialResources, int amount = 1, int? iconOverlap = null, Spr? costUnsatisfiedIcon = null, Spr? costSatisfiedIcon = null, int? iconWidth = null, IKokoroApi.IActionCostApi.CustomCostTooltipProvider? customTooltipProvider = null)
			=> new LegacyCost
			{
				PotentialResources = potentialResources,
				ResourceAmount = amount,
				CostSatisfiedIcon = costSatisfiedIcon,
				CostUnsatisfiedIcon = costUnsatisfiedIcon,
				IconOverlap = iconOverlap,
			};

		public IKokoroApi.IActionCostApi.IActionCost Cost(IKokoroApi.IActionCostApi.IResource resource, int amount = 1, int? iconOverlap = null, IKokoroApi.IActionCostApi.CustomCostTooltipProvider? customTooltipProvider = null)
			=> new LegacyCost
			{
				PotentialResources = [resource],
				ResourceAmount = amount,
				IconOverlap = iconOverlap,
			};

		public IKokoroApi.IActionCostApi.IResource StatusResource(Status status, Spr costUnsatisfiedIcon, Spr costSatisfiedIcon, int? iconWidth = null)
			=> new LegacyResource
			{
				ResourceKey = new ActionCostStatusResource { Status = status, TargetPlayer = true }.ResourceKey,
				CostSatisfiedIcon = costSatisfiedIcon,
				CostUnsatisfiedIcon = costUnsatisfiedIcon,
				Status = status,
				StatusTarget = IKokoroApi.IActionCostApi.StatusResourceTarget.Player,
			};

		public IKokoroApi.IActionCostApi.IResource StatusResource(Status status, IKokoroApi.IActionCostApi.StatusResourceTarget target, Spr costUnsatisfiedIcon, Spr costSatisfiedIcon, int? iconWidth = null)
			=> new LegacyResource
			{
				ResourceKey = new ActionCostStatusResource { Status = status, TargetPlayer = target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player }.ResourceKey,
				CostSatisfiedIcon = costSatisfiedIcon,
				CostUnsatisfiedIcon = costUnsatisfiedIcon,
				Status = status,
				StatusTarget = target,
			};

		public IKokoroApi.IActionCostApi.IResource EnergyResource()
			=> new LegacyResource
			{
				ResourceKey = new ActionCostEnergyResource().ResourceKey,
				CostSatisfiedIcon = (Spr)ModEntry.Instance.Content.EnergyCostSatisfiedSprite.Id!.Value,
				CostUnsatisfiedIcon = (Spr)ModEntry.Instance.Content.EnergyCostUnsatisfiedSprite.Id!.Value,
				IsEnergy = true,
			};

		private static IKokoroApi.IV2.IActionCostsApi.ICost MakeCostFromLegacyData(IKokoroApi.IActionCostApi.IActionCost v1Cost)
		{
			if (v1Cost is LegacyCost legacyCost)
			{
				foreach (var resource in legacyCost.PotentialResources)
				{
					if (resource is not { CostSatisfiedIcon: { } costSatisfiedIcon, CostUnsatisfiedIcon: { } costUnsatisfiedIcon })
						continue;
					if (ActionCostsManager.Instance.ResourceCostIcons.TryGetValue(resource.ResourceKey, out var costIcons) && costIcons.Any(e => e.Amount == 1))
						continue;
					ActionCostsManager.Instance.RegisterResourceCostIcon(resource.ResourceKey, costSatisfiedIcon, costUnsatisfiedIcon);
				}
				
				var v2Cost = new ResourceActionCost
				{
					PotentialResources = legacyCost.PotentialResources.Select(MakeResourceFromLegacyData).ToList(),
					Amount = legacyCost.ResourceAmount,
					DisplayStyle = legacyCost.ResourceAmount >= 5 ? IKokoroApi.IV2.IActionCostsApi.ResourceCostDisplayStyle.IconAndNumber : IKokoroApi.IV2.IActionCostsApi.ResourceCostDisplayStyle.RepeatedIcon,
					ShowOutgoingIcon = legacyCost.PotentialResources.Any(r => r is LegacyResource { StatusTarget: IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow }),
				};
				
				if (legacyCost.IconOverlap is { } iconOverlap)
					v2Cost.Spacing = iconOverlap;
				
				return v2Cost;
			}
			
			return new V1ToV2CostWrapper(v1Cost);
		}

		private static IKokoroApi.IV2.IActionCostsApi.IResource MakeResourceFromLegacyData(IKokoroApi.IActionCostApi.IResource v1Resource)
		{
			if (v1Resource is LegacyResource legacyResource)
			{
				if (legacyResource.IsEnergy)
					return new ActionCostEnergyResource();
				if (legacyResource.Status is { } status)
					return new ActionCostStatusResource { Status = status, TargetPlayer = legacyResource.StatusTarget == IKokoroApi.IActionCostApi.StatusResourceTarget.Player };
				throw new ArgumentException("Invalid legacy resource");
			}
			return new V1ToV2ResourceWrapper(v1Resource);
		}

		internal sealed class LegacyCost : IKokoroApi.IActionCostApi.IActionCost
		{
			public required IReadOnlyList<IKokoroApi.IActionCostApi.IResource> PotentialResources { get; init; }
			public required int ResourceAmount { get; init; }
			public Spr? CostUnsatisfiedIcon { get; init; }
			public Spr? CostSatisfiedIcon { get; init; }
			public int? IconOverlap { get; init; }
			
			public void RenderSingle(G g, ref Vec position, IKokoroApi.IActionCostApi.IResource? satisfiedResource, bool isDisabled, bool dontRender)
				=> throw new InvalidOperationException("V1 API can no longer be used directly, please switch to V2 API");
		}

		internal sealed class LegacyResource : IKokoroApi.IActionCostApi.IResource
		{
			public required string ResourceKey { get; init; }
			public Spr? CostUnsatisfiedIcon { get; init; }
			public Spr? CostSatisfiedIcon { get; init; }
			public Status? Status { get; init; }
			public IKokoroApi.IActionCostApi.StatusResourceTarget StatusTarget { get; init; }
			public bool IsEnergy { get; init; }

			public int GetCurrentResourceAmount(State state, Combat combat)
				=> throw new InvalidOperationException("V1 API can no longer be used directly, please switch to V2 API");

			public void PayResource(State state, Combat combat, int amount)
				=> throw new InvalidOperationException("V1 API can no longer be used directly, please switch to V2 API");

			public void Render(G g, ref Vec position, bool isSatisfied, bool isDisabled, bool dontRender)
				=> throw new InvalidOperationException("V1 API can no longer be used directly, please switch to V2 API");
		}

		internal sealed class V1ToV2CostWrapper(IKokoroApi.IActionCostApi.IActionCost v1) : IKokoroApi.IV2.IActionCostsApi.ICost
		{
			[JsonProperty]
			private readonly IKokoroApi.IActionCostApi.IActionCost V1 = v1;
			
			[JsonIgnore]
			public IReadOnlySet<IKokoroApi.IV2.IActionCostsApi.IResource> MonitoredResources
				=> V1.PotentialResources.Select(MakeResourceFromLegacyData).ToHashSet();

			public IEnumerable<IKokoroApi.IV2.IActionCostsApi.ITransaction> GetPossibleTransactions(IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ICost> context, IKokoroApi.IV2.IActionCostsApi.ITransaction baseTransaction)
			{
				var potentialResources = V1.PotentialResources.Select(MakeResourceFromLegacyData).ToList();
				var newContext = new List<IKokoroApi.IV2.IActionCostsApi.ICost>(context) { this };
				return GetInternalPossibleTransactions(baseTransaction, potentialResources, V1.ResourceAmount);

				IEnumerable<IKokoroApi.IV2.IActionCostsApi.ITransaction> GetInternalPossibleTransactions(IKokoroApi.IV2.IActionCostsApi.ITransaction currentTransaction, List<IKokoroApi.IV2.IActionCostsApi.IResource> potentialResources, int amountLeft)
				{
					if (amountLeft < 0)
						yield break;

					if (amountLeft == 0)
					{
						yield return currentTransaction;
						yield break;
					}

					if (potentialResources.Count == 0)
						yield break;

					if (potentialResources.Count == 1)
					{
						yield return currentTransaction.AddPayment(newContext, potentialResources[0], amountLeft);
						yield break;
					}

					foreach (var resource in potentialResources)
					{
						var newPotentialResources = new List<IKokoroApi.IV2.IActionCostsApi.IResource>(potentialResources);
						newPotentialResources.Remove(resource);
						
						for (var resourceAmount = amountLeft; resourceAmount > 0; resourceAmount--)
							foreach (var transaction in GetInternalPossibleTransactions(currentTransaction.AddPayment(newContext, resource, resourceAmount), newPotentialResources, amountLeft - resourceAmount))
								yield return transaction;
					}
				}
			}

			public void Render(G g, ref Vec position, bool isDisabled, bool dontRender, IKokoroApi.IV2.IActionCostsApi.IWholeTransactionPaymentResult transactionPaymentResult)
			{
				V1.RenderPrefix(g, ref position, isDisabled, dontRender);

				foreach (var payment in transactionPaymentResult.Payments)
				{
					if (payment.Paid != 0)
					{
						var v1Resource = new V2ToV1ResourceWrapper(payment.Payment.Resource);
						for (var i = 0; i < payment.Paid; i++)
							V1.RenderSingle(g, ref position, v1Resource, isDisabled, dontRender);
					}
					
					for (var i = 0; i < payment.Unpaid; i++)
						V1.RenderSingle(g, ref position, null, isDisabled, dontRender);
				}
				
				V1.RenderSuffix(g, ref position, isDisabled, dontRender);
			}

			public IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat)
				=> V1.GetTooltips(state, combat);
		}

		internal sealed class V1ToV2ResourceWrapper(IKokoroApi.IActionCostApi.IResource v1) : IKokoroApi.IV2.IActionCostsApi.IResource
		{
			[JsonProperty]
			private readonly IKokoroApi.IActionCostApi.IResource V1 = v1;
			
			[JsonIgnore]
			public string ResourceKey
				=> V1.ResourceKey;
			
			public int GetCurrentResourceAmount(State state, Combat combat)
				=> V1.GetCurrentResourceAmount(state, combat);

			public void Pay(State state, Combat combat, int amount)
				=> V1.PayResource(state, combat, amount);

			public IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat, int amount)
				=> V1.GetTooltips(state, combat, amount);
		}

		internal sealed class V2ToV1ResourceWrapper(IKokoroApi.IV2.IActionCostsApi.IResource v2) : IKokoroApi.IActionCostApi.IResource
		{
			[JsonProperty]
			private readonly IKokoroApi.IV2.IActionCostsApi.IResource V2 = v2;
			
			[JsonIgnore]
			public string ResourceKey
				=> V2.ResourceKey;

			[JsonIgnore]
			public Spr? CostUnsatisfiedIcon
				=> ActionCostsManager.Instance.GetResourceCostIcons(ResourceKey, 1).FirstOrDefault()?.CostSatisfiedIcon;
			
			[JsonIgnore]
			public Spr? CostSatisfiedIcon
				=> ActionCostsManager.Instance.GetResourceCostIcons(ResourceKey, 1).FirstOrDefault()?.CostUnsatisfiedIcon;
			
			public int GetCurrentResourceAmount(State state, Combat combat)
				=> V2.GetCurrentResourceAmount(state, combat);

			public void PayResource(State state, Combat combat, int amount)
				=> V2.Pay(state, combat, amount);

			public void Render(G g, ref Vec position, bool isSatisfied, bool isDisabled, bool dontRender)
			{
				var icons = ActionCostsManager.Instance.GetResourceCostIcons(ResourceKey, 1);
				if (icons.Count == 0)
					return;
				
				var icon = isSatisfied ? icons[0].CostSatisfiedIcon : icons[0].CostUnsatisfiedIcon;
				var texture = SpriteLoader.Get(icon)!;
				
				if (!dontRender)
					Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
				position.x += texture.Width;
			}
		}
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IActionCostsApi ActionCosts { get; } = new ActionCostsApi();
		
		public sealed class ActionCostsApi : IKokoroApi.IV2.IActionCostsApi
		{
			public void RegisterHook(IKokoroApi.IV2.IActionCostsApi.IHook hook, double priority = 0)
				=> ActionCostsManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IActionCostsApi.IHook hook)
				=> ActionCostsManager.Instance.Unregister(hook);

			public IKokoroApi.IV2.IActionCostsApi.ICostAction? AsCostAction(CardAction action)
				=> action as IKokoroApi.IV2.IActionCostsApi.ICostAction;

			public IKokoroApi.IV2.IActionCostsApi.ICostAction MakeCostAction(IKokoroApi.IV2.IActionCostsApi.ICost cost, CardAction action)
				=> new AResourceCost { Cost = cost, Action = action };

			public void RegisterResourceCostIcon(IKokoroApi.IV2.IActionCostsApi.IResource resource, Spr costSatisfiedIcon, Spr costUnsatisfiedIcon, int amount = 1)
				=> ActionCostsManager.Instance.RegisterResourceCostIcon(resource.ResourceKey, costSatisfiedIcon, costUnsatisfiedIcon, amount);

			public void RegisterStatusResourceCostIcon(Status status, Spr costSatisfiedIcon, Spr costUnsatisfiedIcon, int amount = 1, bool? targetPlayer = null)
			{
				if (targetPlayer is null or true)
					RegisterResourceCostIcon(MakeStatusResource(status, true), costSatisfiedIcon, costUnsatisfiedIcon, amount);
				if (targetPlayer is null or false)
					RegisterResourceCostIcon(MakeStatusResource(status, false), costSatisfiedIcon, costUnsatisfiedIcon, amount);
			}

			public IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.IResourceCostIcon> GetResourceCostIcons(IKokoroApi.IV2.IActionCostsApi.IResource resource, int amount)
				=> ActionCostsManager.Instance.GetResourceCostIcons(resource.ResourceKey, amount);

			public IKokoroApi.IV2.IActionCostsApi.IStatusResource? AsStatusResource(IKokoroApi.IV2.IActionCostsApi.IResource resource)
				=> resource as IKokoroApi.IV2.IActionCostsApi.IStatusResource;

			public IKokoroApi.IV2.IActionCostsApi.IStatusResource MakeStatusResource(Status status, bool targetPlayer = true)
				=> new ActionCostStatusResource { Status = status, TargetPlayer = targetPlayer };

			public IKokoroApi.IV2.IActionCostsApi.IEnergyResource? AsEnergyResource(IKokoroApi.IV2.IActionCostsApi.IResource resource)
				=> resource as IKokoroApi.IV2.IActionCostsApi.IEnergyResource;

			public IKokoroApi.IV2.IActionCostsApi.IEnergyResource EnergyResource
				=> new ActionCostEnergyResource();
			
			public IKokoroApi.IV2.IActionCostsApi.IResourceCost? AsResourceCost(IKokoroApi.IV2.IActionCostsApi.ICost cost)
				=> cost as IKokoroApi.IV2.IActionCostsApi.IResourceCost;

			public IKokoroApi.IV2.IActionCostsApi.IResourceCost MakeResourceCost(IKokoroApi.IV2.IActionCostsApi.IResource resource, int amount)
				=> MakeResourceCost([resource], amount);

			public IKokoroApi.IV2.IActionCostsApi.IResourceCost MakeResourceCost(IEnumerable<IKokoroApi.IV2.IActionCostsApi.IResource> potentialResources, int amount)
			{
				var potentialResourceList = potentialResources.ToList();
				return new ResourceActionCost
				{
					PotentialResources = potentialResourceList,
					Amount = amount,
					DisplayStyle = amount >= 5 ? IKokoroApi.IV2.IActionCostsApi.ResourceCostDisplayStyle.IconAndNumber : IKokoroApi.IV2.IActionCostsApi.ResourceCostDisplayStyle.RepeatedIcon,
					ShowOutgoingIcon = potentialResourceList.All(r => r is IKokoroApi.IV2.IActionCostsApi.IStatusResource { TargetPlayer: false }),
				};
			}

			public IKokoroApi.IV2.IActionCostsApi.ICombinedCost? AsCombinedCost(IKokoroApi.IV2.IActionCostsApi.ICost cost)
				=> cost as IKokoroApi.IV2.IActionCostsApi.ICombinedCost;

			public IKokoroApi.IV2.IActionCostsApi.ICombinedCost MakeCombinedCost(IEnumerable<IKokoroApi.IV2.IActionCostsApi.ICost> costs)
				=> new CombinedResourceActionCost { Costs = costs.ToList() };

			public IKokoroApi.IV2.IActionCostsApi.IMockPaymentEnvironment MakeMockPaymentEnvironment(IKokoroApi.IV2.IActionCostsApi.IPaymentEnvironment? @default = null)
				=> new ActionCostMockPaymentEnvironment(@default);

			public IKokoroApi.IV2.IActionCostsApi.IPaymentEnvironment MakeStatePaymentEnvironment(State state, Combat combat)
				=> new ActionCostStatePaymentEnvironment { State = state, Combat = combat };

			public IKokoroApi.IV2.IActionCostsApi.ITransaction MakeTransaction()
				=> new ActionCostTransaction { Payments = [] };

			public IKokoroApi.IV2.IActionCostsApi.ITransaction GetBestTransaction(IKokoroApi.IV2.IActionCostsApi.ICost cost, IKokoroApi.IV2.IActionCostsApi.IPaymentEnvironment environment)
			{
				var baseTransaction = MakeTransaction();
				var allTransactions = cost.GetPossibleTransactions([], baseTransaction).Cached();
				if (allTransactions.FirstOrDefault(t => t.TestPayment(environment).UnpaidResources.Count == 0) is { } successfulTransaction)
					return successfulTransaction;
				if (!allTransactions.Any())
					return baseTransaction;
				return allTransactions.MinBy(t => t.TestPayment(environment).TotalUnpaid)!;
			}
			
			internal sealed class OnActionCostsTransactionFinishedArgs : IKokoroApi.IV2.IActionCostsApi.IHook.IOnActionCostsTransactionFinishedArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Card? Card { get; internal set; }
				public int Direction { get; internal set; }
				public IKokoroApi.IV2.IActionCostsApi.IWholeTransactionPaymentResult TransactionPaymentResult { get; internal set; } = null!;
			}
		}
	}
}

internal sealed class ActionCostsManager : HookManager<IKokoroApi.IV2.IActionCostsApi.IHook>, IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly ActionCostsManager Instance = new();

	internal readonly Dictionary<string, OrderedList<(int Amount, Spr SatisfiedIcon, Spr UnsatisfiedIcon), int>> ResourceCostIcons = [];
	private readonly HashSet<string> LoggedMissingResourceCostIconWarnings = [];
	private readonly HashSet<string> LoggedImpossibleResourceCostIconWarnings = [];

	private static IKokoroApi.IV2.IActionCostsApi.IMockPaymentEnvironment? CurrentDrawingEnvironment;

	private ActionCostsManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Transpiler)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_Render_Postfix))
		);
	}

	internal static void SetupLate()
	{
		RegisterStatusResourceCostIcon(Status.shard, StableSpr.icons_shardcost, StableSpr.icons_shardcostoff);
		RegisterStatusResourceCostIcon(Status.shield, (Spr)ModEntry.Instance.Content.ShieldCostSatisfiedSprite.Id!.Value, (Spr)ModEntry.Instance.Content.ShieldCostUnsatisfiedSprite.Id!.Value);
		RegisterStatusResourceCostIcon(Status.evade, (Spr)ModEntry.Instance.Content.EvadeCostSatisfiedSprite.Id!.Value, (Spr)ModEntry.Instance.Content.EvadeCostUnsatisfiedSprite.Id!.Value);
		RegisterStatusResourceCostIcon(Status.heat, (Spr)ModEntry.Instance.Content.HeatCostSatisfiedSprite.Id!.Value, (Spr)ModEntry.Instance.Content.HeatCostUnsatisfiedSprite.Id!.Value);
		Instance.RegisterResourceCostIcon(new ActionCostEnergyResource().ResourceKey, (Spr)ModEntry.Instance.Content.EnergyCostSatisfiedSprite.Id!.Value, (Spr)ModEntry.Instance.Content.EnergyCostUnsatisfiedSprite.Id!.Value);

		void RegisterStatusResourceCostIcon(Status status, Spr costSatisfiedIcon, Spr costUnsatisfiedIcon)
		{
			Instance.RegisterResourceCostIcon(new ActionCostStatusResource { Status = status, TargetPlayer = true }.ResourceKey, costSatisfiedIcon, costUnsatisfiedIcon);
			Instance.RegisterResourceCostIcon(new ActionCostStatusResource { Status = status, TargetPlayer = false }.ResourceKey, costSatisfiedIcon, costUnsatisfiedIcon);
		}
	}

	internal void RegisterResourceCostIcon(string resourceKey, Spr costSatisfiedIcon, Spr costUnsatisfiedIcon, int amount = 1)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(amount, 1);

		if (!ResourceCostIcons.TryGetValue(resourceKey, out var icons))
			ResourceCostIcons[resourceKey] = icons = [];

		if (icons.FirstOrNull(e => e.Amount == amount) is { } existingIcon)
			icons.Remove(existingIcon);
		icons.Add((Amount: amount, SatisfiedIcon: costSatisfiedIcon, UnsatisfiedIcon: costUnsatisfiedIcon), amount);
	}

	internal IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.IResourceCostIcon> GetResourceCostIcons(string resourceKey, int amount)
	{
		if (!ResourceCostIcons.TryGetValue(resourceKey, out var icons))
		{
			if (LoggedMissingResourceCostIconWarnings.Add(resourceKey))
				ModEntry.Instance.Logger!.LogError("Requested resource cost icons for resource {Resource}, but no icons are registered for it.", resourceKey);
			return Enumerable.Repeat(new ResourceCostIcon(1, StableSpr.icons_questionMark, StableSpr.icons_missingBooks), amount).ToList();
		}

		if (FindBestResult(null, amount) is { } result)
			return result;
		
		if (LoggedImpossibleResourceCostIconWarnings.Add(resourceKey))
			ModEntry.Instance.Logger!.LogError("Requested resource cost icons for {Amount} of resource {Resource}, but the registered icons cannot produce that amount.", amount, resourceKey);
		return Enumerable.Repeat(new ResourceCostIcon(1, StableSpr.icons_questionMark, StableSpr.icons_missingBooks), amount).ToList();

		List<IKokoroApi.IV2.IActionCostsApi.IResourceCostIcon>? FindBestResult(List<IKokoroApi.IV2.IActionCostsApi.IResourceCostIcon>? current, int amountLeft)
		{
			if (amountLeft <= 0)
				return current;

			foreach (var e in icons.Entries.Reverse())
			{
				var newCurrent = current?.ToList() ?? [];
				newCurrent.Add(new ResourceCostIcon(e.OrderingValue, e.Element.SatisfiedIcon, e.Element.UnsatisfiedIcon));
				if (FindBestResult(newCurrent, amountLeft - e.OrderingValue) is { } result)
					return result;
			}

			return null;
		}
	}
	
	internal void OnActionCostsTransactionFinished(State state, Combat combat, Card? card, IKokoroApi.IV2.IActionCostsApi.IWholeTransactionPaymentResult transactionPaymentResult)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.ActionCostsApi.OnActionCostsTransactionFinishedArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;
			args.TransactionPaymentResult = transactionPaymentResult;
		
			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				hook.OnActionCostsTransactionFinished(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}
	
	public IEnumerable<CardAction>? GetWrappedCardActions(IKokoroApi.IV2.IWrappedActionsApi.IHook.IGetWrappedCardActionsArgs args)
	{
		if (args.Action is not AResourceCost resourceCostAction)
			return null;
		if (resourceCostAction.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}

	private static void Card_GetActionsOverridden_Postfix(Card __instance, ref List<CardAction> __result)
	{
		foreach (var action in __result.SelectMany(a => WrappedActionManager.Instance.GetWrappedCardActionsRecursively(a, includingWrapperActions: true)))
		{
			if (action is not AResourceCost resourceCostAction)
				continue;
			resourceCostAction.CardId = __instance.uuid;
		}
	}

	private static void Card_Render_Prefix()
		=> CurrentDrawingEnvironment = null;
	
	private static IEnumerable<CodeInstruction> Card_MakeAllActionIcons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("GetActionsOverridden"))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_MakeAllActionIcons_Transpiler_ModifyActions)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static List<CardAction> Card_MakeAllActionIcons_Transpiler_ModifyActions(List<CardAction> actions, Card card, State state)
	{
		var energyResource = actions
			.SelectMany(a => WrappedActionManager.Instance.GetWrappedCardActionsRecursively(a, includingWrapperActions: true))
			.OfType<AResourceCost>()
			.Select(a => a.Cost)
			.SelectMany(c => c.MonitoredResources)
			.OfType<IKokoroApi.IV2.IActionCostsApi.IEnergyResource>()
			.FirstOrDefault();

		var stateEnvironment = ModEntry.Instance.Api.V2.ActionCosts.MakeStatePaymentEnvironment(state, state.route as Combat ?? DB.fakeCombat);
		var drawingEnvironment = ModEntry.Instance.Api.V2.ActionCosts.MakeMockPaymentEnvironment(stateEnvironment);
		if (energyResource is not null)
			drawingEnvironment.SetAvailableResource(energyResource, drawingEnvironment.GetAvailableResource(energyResource) - card.GetDataWithOverrides(state).cost);

		CurrentDrawingEnvironment = drawingEnvironment;
		return actions;
	}
	
	private static void Card_MakeAllActionIcons_Finalizer()
		=> CurrentDrawingEnvironment = null;
	
	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not AResourceCost resourceCostAction)
			return true;
		if (resourceCostAction.Action is not { } wrappedAction)
			return false;
		
		var environment = CurrentDrawingEnvironment ?? ModEntry.Instance.Api.V2.ActionCosts.MakeMockPaymentEnvironment();
		if (dontDraw)
			environment = ModEntry.Instance.Api.V2.ActionCosts.MakeMockPaymentEnvironment(environment);

		var oldActionDisabled = wrappedAction.disabled;
		wrappedAction.disabled = action.disabled;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		var transaction = ModEntry.Instance.Api.V2.ActionCosts.GetBestTransaction(resourceCostAction.Cost, environment);
		var transactionPaymentResult = transaction.Pay(environment);
		resourceCostAction.Cost.Render(g, ref position, action.disabled, dontDraw, transactionPaymentResult);

		position.x += 2;
		if (wrappedAction is AAttack attack)
		{
			var shouldStun = state.EnumerateAllArtifacts().Any(a => a.ModifyAttacksToStun(state, state.route as Combat) == true);
			if (shouldStun)
				attack.stunEnemy = shouldStun;
		}

		g.Push(rect: new(position.x - initialX));
		position.x += Card.RenderAction(g, state, wrappedAction, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		wrappedAction.disabled = oldActionDisabled;

		return false;
	}
	
	private static void State_Render_Postfix()
		=> CurrentDrawingEnvironment = null;
}

internal record ResourceCostIcon(
	int Amount,
	Spr CostSatisfiedIcon,
	Spr CostUnsatisfiedIcon
) : IKokoroApi.IV2.IActionCostsApi.IResourceCostIcon;

internal sealed class ActionCostMockPaymentEnvironment(IKokoroApi.IV2.IActionCostsApi.IPaymentEnvironment? @default) : IKokoroApi.IV2.IActionCostsApi.IMockPaymentEnvironment
{
	private readonly Dictionary<string, int> MockResourceAmounts = [];

	public int GetAvailableResource(IKokoroApi.IV2.IActionCostsApi.IResource resource)
	{
		if (!MockResourceAmounts.TryGetValue(resource.ResourceKey, out var amount))
			MockResourceAmounts[resource.ResourceKey] = amount = @default?.GetAvailableResource(resource) ?? 0;
		return amount;
	}

	public bool TryPayResource(IKokoroApi.IV2.IActionCostsApi.IResource resource, int amount)
	{
		var available = GetAvailableResource(resource);
		if (available < amount)
			return false;

		SetAvailableResource(resource, available - amount);
		return true;
	}

	public IKokoroApi.IV2.IActionCostsApi.IMockPaymentEnvironment SetAvailableResource(IKokoroApi.IV2.IActionCostsApi.IResource resource, int amount)
	{
		MockResourceAmounts[resource.ResourceKey] = amount;
		return this;
	}
}

internal sealed class ActionCostStatePaymentEnvironment : IKokoroApi.IV2.IActionCostsApi.IPaymentEnvironment
{
	public required State State { get; init; }
	public required Combat Combat { get; init; }

	public int GetAvailableResource(IKokoroApi.IV2.IActionCostsApi.IResource resource)
		=> resource.GetCurrentResourceAmount(State, Combat);

	public bool TryPayResource(IKokoroApi.IV2.IActionCostsApi.IResource resource, int amount)
	{
		if (Combat == DB.fakeCombat)
			return false;
		
		resource.Pay(State, Combat, amount);
		return true;
	}
}

internal sealed class ActionCostTransaction : IKokoroApi.IV2.IActionCostsApi.ITransaction
{
	public required IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ITransactionPayment> Payments { get; init; }

	public IReadOnlyDictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int> Resources
		=> LazyResources.Value;
	
	private readonly Lazy<IReadOnlyDictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int>> LazyResources;

	public ActionCostTransaction()
	{
		this.LazyResources = new(() =>
		{
			var result = new Dictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int>();
			foreach (var payment in Payments!)
			{
				if (payment.Amount <= 0)
					continue;
				result[payment.Resource] = result.GetValueOrDefault(payment.Resource) + payment.Amount;
			}
			return result;
		});
	}
	
	public IKokoroApi.IV2.IActionCostsApi.IWholeTransactionPaymentResult TestPayment(IKokoroApi.IV2.IActionCostsApi.IPaymentEnvironment environment)
	{
		var mockEnvironment = ModEntry.Instance.Api.V2.ActionCosts.MakeMockPaymentEnvironment(environment);
		return Pay(mockEnvironment);
	}

	public IKokoroApi.IV2.IActionCostsApi.IWholeTransactionPaymentResult Pay(IKokoroApi.IV2.IActionCostsApi.IPaymentEnvironment environment)
	{
		var paymentResults = new List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>();

		foreach (var payment in Payments)
		{
			var available = environment.GetAvailableResource(payment.Resource);
			var paid = Math.Min(payment.Amount, available);
			var unpaid = payment.Amount - paid;

			if (!environment.TryPayResource(payment.Resource, paid))
			{
				paid = 0;
				unpaid = payment.Amount;
			}
			
			paymentResults.Add(new ActionCostTransactionPaymentResult(payment, paid, unpaid));
		}
		
		return new ActionCostWholeTransactionPaymentResult { Transaction = this, Payments = paymentResults };
	}

	public IKokoroApi.IV2.IActionCostsApi.ITransaction AddPayment(IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ICost> context, IKokoroApi.IV2.IActionCostsApi.IResource resource, int amount)
	{
		var newPayments = new List<IKokoroApi.IV2.IActionCostsApi.ITransactionPayment>(Payments.Count + 1);
		newPayments.AddRange(Payments);
		newPayments.Add(new ActionCostTransactionPayment(context, resource, amount));
		return new ActionCostTransaction { Payments = newPayments };
	}
}

internal record ActionCostTransactionPayment(
	IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ICost> Context,
	IKokoroApi.IV2.IActionCostsApi.IResource Resource,
	int Amount
) : IKokoroApi.IV2.IActionCostsApi.ITransactionPayment;

internal record ActionCostTransactionPaymentResult(
	IKokoroApi.IV2.IActionCostsApi.ITransactionPayment Payment,
	int Paid,
	int Unpaid
) : IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult;

internal sealed class ActionCostWholeTransactionPaymentResult : IKokoroApi.IV2.IActionCostsApi.IWholeTransactionPaymentResult
{
	public required IKokoroApi.IV2.IActionCostsApi.ITransaction Transaction { get; init; }
	public required IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult> Payments { get; init; }

	public IReadOnlyDictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int> PaidResources
		=> LazyPaidResources.Value;

	public IReadOnlyDictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int> UnpaidResources
		=> LazyUnpaidResources.Value;
	
	public int TotalPaid
		=> LazyTotalPaid.Value;
	
	public int TotalUnpaid
		=> LazyTotalUnpaid.Value;

	private readonly Lazy<IReadOnlyDictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int>> LazyPaidResources;
	private readonly Lazy<IReadOnlyDictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int>> LazyUnpaidResources;
	private readonly Lazy<int> LazyTotalPaid;
	private readonly Lazy<int> LazyTotalUnpaid;

	public ActionCostWholeTransactionPaymentResult()
	{
		this.LazyPaidResources = new(() =>
		{
			var result = new Dictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int>();
			foreach (var payment in Payments!)
				if (payment.Paid > 0)
					result[payment.Payment.Resource] = result.GetValueOrDefault(payment.Payment.Resource) + payment.Paid;
			return result;
		});
		this.LazyUnpaidResources = new(() =>
		{
			var result = new Dictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int>();
			foreach (var payment in Payments!)
				if (payment.Unpaid > 0)
					result[payment.Payment.Resource] = result.GetValueOrDefault(payment.Payment.Resource) + payment.Unpaid;
			return result;
		});
		this.LazyTotalPaid = new(() => PaidResources.Values.Sum());
		this.LazyTotalUnpaid = new(() => UnpaidResources.Values.Sum());
	}
}

internal sealed class AResourceCost : CardAction, IKokoroApi.IV2.IActionCostsApi.ICostAction
{
	public required IKokoroApi.IV2.IActionCostsApi.ICost Cost { get; set; }
	public required CardAction Action { get; set; }
	public IDictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int> ResourceAmountDisplayOverrides { get; set; } = new Dictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int>();
	public int? CardId { get; internal set; }
	
	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		var environment = ModEntry.Instance.Api.V2.ActionCosts.MakeStatePaymentEnvironment(s, c);
		var baseTransaction = ModEntry.Instance.Api.V2.ActionCosts.MakeTransaction();
		if (ChooseBestTransaction() is not { } transaction)
			return;

		var paymentResult = transaction.Pay(environment);
		if (paymentResult.UnpaidResources.Count != 0)
		{
			ModEntry.Instance.Logger!.LogError("[ActionCosts] Test for transaction {Transaction} for action {Action} succeeded, but payment itself failed: {Result}", transaction, Action, paymentResult);
			return;
		}
		
		c.QueueImmediate(Action);

		var card = CardId is { } cardId ? s.FindCard(cardId) : null;
		ActionCostsManager.Instance.OnActionCostsTransactionFinished(s, c, card, paymentResult);

		IKokoroApi.IV2.IActionCostsApi.ITransaction? ChooseBestTransaction()
			=> Cost.GetPossibleTransactions([], baseTransaction)
				.FirstOrDefault(t => t.TestPayment(environment).UnpaidResources.Count == 0);
	}

	public override List<Tooltip> GetTooltips(State s)
		=> [
			.. Cost.GetTooltips(s, s.route as Combat ?? DB.fakeCombat),
			.. Action.GetTooltips(s)
		];
	
	public IKokoroApi.IV2.IActionCostsApi.ICostAction SetCost(IKokoroApi.IV2.IActionCostsApi.ICost value)
	{
		Cost = value;
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.ICostAction SetAction(CardAction value)
	{
		Action = value;
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.ICostAction SetResourceAmountDisplayOverride(IKokoroApi.IV2.IActionCostsApi.IResource resource, int amount)
	{
		ResourceAmountDisplayOverrides[resource] = amount;
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.ICostAction SetResourceAmountDisplayOverrides(IDictionary<IKokoroApi.IV2.IActionCostsApi.IResource, int> value)
	{
		ResourceAmountDisplayOverrides = value;
		return this;
	}
}

internal sealed class ResourceActionCost : IKokoroApi.IV2.IActionCostsApi.IResourceCost
{
	public required IList<IKokoroApi.IV2.IActionCostsApi.IResource> PotentialResources { get; set; }
	public required int Amount { get; set; }
	public IKokoroApi.IV2.IActionCostsApi.ResourceCostDisplayStyle DisplayStyle { get; set; }
	public int Spacing { get; set; } = -3;
	public bool ShowOutgoingIcon { get; set; }
	
	[JsonIgnore]
	public IReadOnlyList<Spr>? CostSatisfiedIconOverride { get; set; }
	
	[JsonIgnore]
	public IReadOnlyList<Spr>? CostUnsatisfiedIconOverride { get; set; }

	[JsonIgnore]
	public IReadOnlySet<IKokoroApi.IV2.IActionCostsApi.IResource> MonitoredResources
		=> PotentialResources.ToHashSet();
	
	public IEnumerable<IKokoroApi.IV2.IActionCostsApi.ITransaction> GetPossibleTransactions(IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ICost> context, IKokoroApi.IV2.IActionCostsApi.ITransaction baseTransaction)
	{
		var potentialResources = PotentialResources as List<IKokoroApi.IV2.IActionCostsApi.IResource> ?? [.. PotentialResources];
		var newContext = new List<IKokoroApi.IV2.IActionCostsApi.ICost>(context) { this };
		return GetInternalPossibleTransactions(baseTransaction, potentialResources, Amount);

		IEnumerable<IKokoroApi.IV2.IActionCostsApi.ITransaction> GetInternalPossibleTransactions(IKokoroApi.IV2.IActionCostsApi.ITransaction currentTransaction, List<IKokoroApi.IV2.IActionCostsApi.IResource> potentialResources, int amountLeft)
		{
			if (amountLeft < 0)
				yield break;
			
			if (amountLeft == 0)
			{
				yield return currentTransaction;
				yield break;
			}
			
			if (potentialResources.Count == 0)
				yield break;
			
			if (potentialResources.Count == 1)
			{
				yield return currentTransaction.AddPayment(newContext, potentialResources[0], amountLeft);
				yield break;
			}
			
			foreach (var resource in potentialResources)
			{
				var newPotentialResources = new List<IKokoroApi.IV2.IActionCostsApi.IResource>(potentialResources);
				newPotentialResources.Remove(resource);
						
				for (var resourceAmount = amountLeft; resourceAmount > 0; resourceAmount--)
					foreach (var transaction in GetInternalPossibleTransactions(currentTransaction.AddPayment(newContext, resource, resourceAmount), newPotentialResources, amountLeft - resourceAmount))
						yield return transaction;
			}
		}
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender, IKokoroApi.IV2.IActionCostsApi.IWholeTransactionPaymentResult transactionPaymentResult)
	{
		if (PotentialResources.Count == 0)
			return;
		
		if (ShowOutgoingIcon)
		{
			if (!dontRender)
				Draw.Sprite(StableSpr.icons_outgoing, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += 8;
		}
		
		var relatedPayments = transactionPaymentResult.Payments.Where(p => p.Payment.Context.LastOrDefault() == this).ToList();
		relatedPayments = MinimizePayments(relatedPayments);

		switch (DisplayStyle)
		{
			case IKokoroApi.IV2.IActionCostsApi.ResourceCostDisplayStyle.RepeatedIcon:
			{
				var isFirst = true;
				foreach (var payment in relatedPayments)
				{
					foreach (var icon in GetIcons())
					{
						if (!isFirst)
							position.x += Spacing;
					
						if (!dontRender)
							Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
					
						var texture = SpriteLoader.Get(icon)!;
						position.x += texture.Width;
						isFirst = false;
					}
					
					IReadOnlyList<Spr> GetIcons()
					{
						var iconsResult = new List<Spr>();

						if (payment.Paid != 0)
						{
							if (CostSatisfiedIconOverride is { } icons)
								iconsResult.AddRange(icons);
							else
								iconsResult.AddRange(ActionCostsManager.Instance.GetResourceCostIcons(payment.Payment.Resource.ResourceKey, payment.Paid).Select(i => i.CostSatisfiedIcon));
						}
						
						if (payment.Unpaid != 0)
						{
							if (CostUnsatisfiedIconOverride is { } icons)
								iconsResult.AddRange(icons);
							else
								iconsResult.AddRange(ActionCostsManager.Instance.GetResourceCostIcons(payment.Payment.Resource.ResourceKey, payment.Unpaid).Select(i => i.CostUnsatisfiedIcon));
						}

						return iconsResult;
					}
				}
				
				break;
			}
			case IKokoroApi.IV2.IActionCostsApi.ResourceCostDisplayStyle.IconAndNumber:
			{
				var icons = GetIcons();
				if (icons.Count == 0)
					break;
				
				var isFirst = true;
				foreach (var icon in icons)
				{
					if (!isFirst)
						position.x += Spacing;
					
					if (!dontRender)
						Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
					
					var texture = SpriteLoader.Get(icon)!;
					position.x += texture.Width;
					isFirst = false;
				}

				position.x += 1;
				
				if (!dontRender)
					BigNumbers.Render(Amount, position.x, position.y, isDisabled ? Colors.disabledText : Colors.textMain);
				position.x += Amount.ToString().Length * 6;
				
				break;

				IReadOnlyList<Spr> GetIcons()
				{
					if (relatedPayments.All(p => p.Unpaid == 0))
					{
						if (CostSatisfiedIconOverride is { } iconOverride)
							return iconOverride;

						var iconsResult = new List<Spr>();
						foreach (var paidResource in relatedPayments.Select(p => p.Payment.Resource).DistinctBy(r => r.ResourceKey))
						{
							var icons = ActionCostsManager.Instance.GetResourceCostIcons(paidResource.ResourceKey, 1);
							if (icons.Count != 0)
								iconsResult.Add(icons[0].CostSatisfiedIcon);
						}
						
						return iconsResult;
					}
					else
					{
						if (CostUnsatisfiedIconOverride is { } iconOverride)
							return iconOverride;

						var icons = ActionCostsManager.Instance.GetResourceCostIcons(PotentialResources[0].ResourceKey, 1);
						return icons.Count == 0 ? [] : [icons[0].CostUnsatisfiedIcon];
					}
				}
			}
			default:
				throw new ArgumentOutOfRangeException();
		}
		
		List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult> MinimizePayments(List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult> payments)
		{
			IKokoroApi.IV2.IActionCostsApi.IResource? currentResource = null;
			var currentPaymentResults = new List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>();
			var newPaymentResults = new List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult>();
		
			foreach (var payment in payments)
			{
				if (currentResource is not null && currentResource.ResourceKey != payment.Payment.Resource.ResourceKey)
					FinishCurrent();
		
				currentResource = payment.Payment.Resource;
				currentPaymentResults.Add(payment);
			}
				
			FinishCurrent();
		
			return newPaymentResults;
		
			void FinishCurrent()
			{
				if (currentResource is not null && currentPaymentResults.Count != 0)
					newPaymentResults.Add(CombinePayments(currentResource, currentPaymentResults));
				currentResource = null;
				currentPaymentResults.Clear();
			}
		
			IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult CombinePayments(IKokoroApi.IV2.IActionCostsApi.IResource resource, List<IKokoroApi.IV2.IActionCostsApi.ITransactionPaymentResult> payments)
			{
				var paid = payments.Sum(p => p.Paid);
				var unpaid = payments.Sum(p => p.Unpaid);
				var payment = new ActionCostTransactionPayment(GetCommonContext(payments.Select(p => p.Payment).ToList()), resource, paid + unpaid);
				return new ActionCostTransactionPaymentResult(payment, paid, unpaid);
			}
		
			List<IKokoroApi.IV2.IActionCostsApi.ICost> GetCommonContext(List<IKokoroApi.IV2.IActionCostsApi.ITransactionPayment> payments)
			{
				var max = payments.Max(p => p.Context.Count);
				for (var i = max - 1; i >= 0; i--)
				{
					if (payments.Select(p => p.Context[i]).ToHashSet().Count != 1)
						continue;
					return payments[0].Context.Take(i).ToList();
				}
				return [];
			}
		}
	}

	public IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat)
		=> PotentialResources.SelectMany(r => r.GetTooltips(state, combat, Amount)).ToList();
	
	public IKokoroApi.IV2.IActionCostsApi.IResourceCost SetPotentialResources(IEnumerable<IKokoroApi.IV2.IActionCostsApi.IResource> value)
	{
		PotentialResources = value.ToList();
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.IResourceCost SetAmount(int value)
	{
		Amount = value;
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.IResourceCost SetDisplayStyle(IKokoroApi.IV2.IActionCostsApi.ResourceCostDisplayStyle value)
	{
		DisplayStyle = value;
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.IResourceCost SetSpacing(int value)
	{
		Spacing = value;
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.IResourceCost SetShowOutgoingIcon(bool value)
	{
		ShowOutgoingIcon = value;
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.IResourceCost SetCostSatisfiedIconOverride(IReadOnlyList<Spr>? value)
	{
		CostSatisfiedIconOverride = value;
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.IResourceCost SetCostUnsatisfiedIconOverride(IReadOnlyList<Spr>? value)
	{
		CostUnsatisfiedIconOverride = value;
		return this;
	}
}

internal abstract class BaseActionCostResource : IKokoroApi.IV2.IActionCostsApi.IResource
{
	public abstract string ResourceKey { get; }
	
	public abstract int GetCurrentResourceAmount(State state, Combat combat);
	public abstract void Pay(State state, Combat combat, int amount);
	public abstract IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat, int amount);
	
	public override bool Equals(object? obj)
		=> obj is BaseActionCostResource resource && resource.ResourceKey == ResourceKey;

	public override int GetHashCode()
		=> ResourceKey.GetHashCode();
}

internal sealed class ActionCostStatusResource : BaseActionCostResource, IKokoroApi.IV2.IActionCostsApi.IStatusResource
{
	public required Status Status { get; init; }
	public required bool TargetPlayer { get; init; }

	[JsonIgnore]
	public override string ResourceKey
		=> $"actioncost.resource.status.{Status.Key()}.{(TargetPlayer ? "player" : "enemy")}";

	public override int GetCurrentResourceAmount(State state, Combat combat)
	{
		var ship = TargetPlayer ? state.ship : combat.otherShip;
		return ship.Get(Status);
	}

	public override void Pay(State state, Combat combat, int amount)
	{
		var ship = TargetPlayer ? state.ship : combat.otherShip;
		ship.Add(Status, -amount);
	}

	public override IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat, int amount)
	{
		if (amount <= 0)
			return [];
		
		var nameFormat = ModEntry.Instance.Localizations.Localize(["resourceCost", "status", TargetPlayer ? "player" : "enemy", "name"]);
		var descriptionFormat = ModEntry.Instance.Localizations.Localize(["resourceCost", "status", TargetPlayer ? "player" : "enemy", "description"]);
		var icon = ModEntry.Instance.Api.V2.ActionCosts.GetResourceCostIcons(this, amount)[0].CostSatisfiedIcon;
		var name = string.Format(nameFormat, Status.GetLocName().ToUpper());
		var description = string.Format(descriptionFormat, amount, Status.GetLocName().ToUpper());

		return [
			new GlossaryTooltip(ResourceKey)
			{
				Icon = icon,
				TitleColor = Colors.action,
				Title = name,
				Description = description,
			}
		];
	}
}

internal sealed class ActionCostEnergyResource : BaseActionCostResource, IKokoroApi.IV2.IActionCostsApi.IEnergyResource
{
	[JsonIgnore]
	public override string ResourceKey
		=> "actioncost.resource.energy";

	public override int GetCurrentResourceAmount(State state, Combat combat)
		=> combat.energy;

	public override void Pay(State state, Combat combat, int amount)
		=> combat.energy = Math.Max(combat.energy - amount, 0);

	public override IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat, int amount)
	{
		if (amount <= 0)
			return [];
		
		var icon = ModEntry.Instance.Api.V2.ActionCosts.GetResourceCostIcons(this, amount)[0].CostSatisfiedIcon;
		return [
			new GlossaryTooltip(ResourceKey)
			{
				Icon = icon,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["resourceCost", "energy", "name"]),
				Description = string.Format(ModEntry.Instance.Localizations.Localize(["resourceCost", "energy", "description"]), amount),
			}
		];
	}
}

internal sealed class CombinedResourceActionCost : IKokoroApi.IV2.IActionCostsApi.ICombinedCost
{
	public required IList<IKokoroApi.IV2.IActionCostsApi.ICost> Costs { get; set; }
	public int Spacing { get; set; } = 1;

	[JsonIgnore]
	public IReadOnlySet<IKokoroApi.IV2.IActionCostsApi.IResource> MonitoredResources
		=> Costs.SelectMany(c => c.MonitoredResources).ToHashSet();

	public IEnumerable<IKokoroApi.IV2.IActionCostsApi.ITransaction> GetPossibleTransactions(IReadOnlyList<IKokoroApi.IV2.IActionCostsApi.ICost> context, IKokoroApi.IV2.IActionCostsApi.ITransaction baseTransaction)
	{
		return GetAll(Costs, baseTransaction);
		
		IEnumerable<IKokoroApi.IV2.IActionCostsApi.ITransaction> GetAll(IList<IKokoroApi.IV2.IActionCostsApi.ICost> costsLeft, IKokoroApi.IV2.IActionCostsApi.ITransaction currentTransaction)
		{
			if (costsLeft.Count == 0)
			{
				yield return currentTransaction;
				yield break;
			}

			var newCostsLeft = costsLeft.ToList();
			newCostsLeft.RemoveAt(0);
			var newContext = new List<IKokoroApi.IV2.IActionCostsApi.ICost>(context) { this };

			foreach (var newCurrentTransaction in costsLeft[0].GetPossibleTransactions(newContext, currentTransaction))
				foreach (var transaction in GetAll(newCostsLeft, newCurrentTransaction))
					yield return transaction;
		}
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender, IKokoroApi.IV2.IActionCostsApi.IWholeTransactionPaymentResult transactionPaymentResult)
	{
		var isFirst = true;

		foreach (var cost in Costs)
		{
			if (!isFirst)
				position.x += Spacing;
			
			cost.Render(g, ref position, isDisabled, dontRender, transactionPaymentResult);
			isFirst = false;
		}
	}

	public IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat)
		=> Costs.SelectMany(c => c.GetTooltips(state, combat)).ToList();
	
	public IKokoroApi.IV2.IActionCostsApi.ICombinedCost SetCosts(IEnumerable<IKokoroApi.IV2.IActionCostsApi.ICost> value)
	{
		Costs = value.ToList();
		return this;
	}

	public IKokoroApi.IV2.IActionCostsApi.ICombinedCost SetSpacing(int value)
	{
		Spacing = value;
		return this;
	}
}