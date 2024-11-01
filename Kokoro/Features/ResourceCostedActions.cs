using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public IKokoroApi.IActionCostApi ActionCosts { get; } = new ActionCostApiImplementation();
	
	public sealed class ActionCostApiImplementation : IKokoroApi.IActionCostApi
	{
		public CardAction Make(IKokoroApi.IActionCostApi.IActionCost cost, CardAction action)
			=> new AResourceCost { Costs = [cost], Action = action };

		public CardAction Make(IReadOnlyList<IKokoroApi.IActionCostApi.IActionCost> costs, CardAction action)
			=> new AResourceCost { Costs = costs.ToList(), Action = action };

		public IKokoroApi.IActionCostApi.IActionCost Cost(IReadOnlyList<IKokoroApi.IActionCostApi.IResource> potentialResources, int amount = 1, int? iconOverlap = null, Spr? costUnsatisfiedIcon = null, Spr? costSatisfiedIcon = null, int? iconWidth = null, IKokoroApi.IActionCostApi.CustomCostTooltipProvider? customTooltipProvider = null)
			=> new ActionCostImpl(potentialResources, amount, iconOverlap, costUnsatisfiedIcon, costSatisfiedIcon, iconWidth, customTooltipProvider);

		public IKokoroApi.IActionCostApi.IActionCost Cost(IKokoroApi.IActionCostApi.IResource resource, int amount = 1, int? iconOverlap = null, IKokoroApi.IActionCostApi.CustomCostTooltipProvider? customTooltipProvider = null)
			=> new ActionCostImpl(new List<IKokoroApi.IActionCostApi.IResource> () { resource }, amount, iconOverlap, null, null, null, customTooltipProvider);

		public IKokoroApi.IActionCostApi.IResource StatusResource(Status status, Spr costUnsatisfiedIcon, Spr costSatisfiedIcon, int? iconWidth = null)
			=> new ActionCostStatusResource(status, target: IKokoroApi.IActionCostApi.StatusResourceTarget.Player, costUnsatisfiedIcon, costSatisfiedIcon, iconWidth);

		public IKokoroApi.IActionCostApi.IResource StatusResource(Status status, IKokoroApi.IActionCostApi.StatusResourceTarget target, Spr costUnsatisfiedIcon, Spr costSatisfiedIcon, int? iconWidth = null)
			=> new ActionCostStatusResource(status, target, costUnsatisfiedIcon, costSatisfiedIcon, iconWidth);

		public IKokoroApi.IActionCostApi.IResource EnergyResource()
			=> new ActionCostEnergyResource();
	}
}

internal sealed class ResourceCostedActionManager : IKokoroApi.IV2.IWrappedActionsApi.IHook
{
	internal static readonly ResourceCostedActionManager Instance = new();
	
	private static Dictionary<string, int>? CurrentResourceState;
	private static Dictionary<string, int>? CurrentNonDrawingResourceState;
	
	internal static void Setup(IHarmony harmony)
	{
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
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_Render_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
	}
	
	public IEnumerable<CardAction>? GetWrappedCardActions(CardAction action)
	{
		if (action is not AResourceCost resourceCostAction)
			return null;
		if (resourceCostAction.Action is not { } wrappedAction)
			return null;
		return [wrappedAction];
	}

	private static void Card_Render_Prefix()
	{
		CurrentResourceState = null;
		CurrentNonDrawingResourceState = null;
	}
	
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
		var resources = actions
			.SelectMany(a => WrappedActionManager.Instance.GetWrappedCardActionsRecursively(a, includingWrapperActions: true))
			.OfType<AResourceCost>()
			.SelectMany(a => a.Costs ?? [])
			.SelectMany(c => c.PotentialResources)
			.ToList();

		CurrentResourceState = resources.Count == 0 ? [] : AResourceCost.GetCurrentResourceState(state, state.route as Combat ?? DB.fakeCombat, resources);
		if (CurrentResourceState.ContainsKey("Energy"))
			CurrentResourceState["Energy"] -= card.GetDataWithOverrides(state).cost;
		CurrentNonDrawingResourceState = new(CurrentResourceState);

		return actions;
	}
	
	private static void Card_MakeAllActionIcons_Finalizer()
	{
		CurrentResourceState = null;
		CurrentNonDrawingResourceState = null;
	}
	
	private static void State_Render_Postfix()
	{
		CurrentResourceState = null;
		CurrentNonDrawingResourceState = null;
	}
	
	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is not AResourceCost resourceCostAction)
			return true;
		if (resourceCostAction.Action is not { } wrappedAction)
			return false;
		var resourceState = (dontDraw ? CurrentNonDrawingResourceState : CurrentResourceState) ?? new();

		var oldActionDisabled = wrappedAction.disabled;
		wrappedAction.disabled = action.disabled;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		var (payment, groupedPayment, _) = AResourceCost.GetResourcePayment(resourceState, resourceCostAction.Costs ?? []);
		resourceCostAction.RenderCosts(g, ref position, action.disabled, dontDraw, payment);
		if (!action.disabled)
			foreach (var (resourceKey, resourceAmount) in groupedPayment)
				resourceState[resourceKey] = resourceState.GetValueOrDefault(resourceKey) - resourceAmount;

		position.x += 2;
		if (wrappedAction is AAttack attack)
		{
			var shouldStun = state.EnumerateAllArtifacts().Any(a => a.ModifyAttacksToStun(state, state.route as Combat) == true);
			if (shouldStun)
				attack.stunEnemy = shouldStun;
		}

		g.Push(rect: new(position.x - initialX, 0));
		position.x += Card.RenderAction(g, state, wrappedAction, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
		g.Pop();

		__result = (int)position.x - initialX;
		g.Pop();
		wrappedAction.disabled = oldActionDisabled;

		return false;
	}
}

public sealed class AResourceCost : CardAction
{
	public List<IKokoroApi.IActionCostApi.IActionCost>? Costs;
	public CardAction? Action;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Action is null)
			return;
		if (Costs is null)
		{
			c.QueueImmediate(Action);
			return;
		}

		var resourceState = GetCurrentResourceState(s, c, Costs.SelectMany(c => c.PotentialResources));
		var (_, resourcePayment, canPay) = GetResourcePayment(resourceState, Costs);

		if (!canPay)
			return;

		foreach (var (resourceKey, amount) in resourcePayment)
		{
			var resource = Costs.SelectMany(c => c.PotentialResources).First(r => r.ResourceKey == resourceKey);
			resource.PayResource(s, c, amount);
		}
		c.QueueImmediate(Action);
	}

	internal void RenderCosts(G g, ref Vec position, bool isDisabled, bool dontRender, List<(string ResourceKey, bool IsSatisfied)> payment)
	{
		if (Costs is null)
			return;

		var resourceIndex = 0;
		Costs.FirstOrDefault()?.RenderPrefix(g, ref position, isDisabled, dontRender);
		foreach (var cost in Costs)
		{
			for (var i = 0; i < cost.ResourceAmount; i++)
			{
				var (resourceKey, isResourceSatisfied) = payment[resourceIndex];
				var resource = cost.PotentialResources.FirstOrDefault(r => r.ResourceKey == resourceKey);
				cost.RenderSingle(g, ref position, isResourceSatisfied ? resource : null, isDisabled, dontRender);
				position.x -= 2;
				resourceIndex++;
			}
		}
		position.x += 2;
		Costs.LastOrDefault()?.RenderSuffix(g, ref position, isDisabled, dontRender);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		List<Tooltip> tooltips = [];
		if (Costs is not null)
			foreach (var cost in Costs)
				tooltips.AddRange(cost.GetTooltips(s, s.route as Combat));
		if (Action is not null)
			tooltips.AddRange(Action.GetTooltips(s));
		return tooltips;
	}

	public static Dictionary<string, int> GetCurrentResourceState(State state, Combat combat, IEnumerable<IKokoroApi.IActionCostApi.IResource> potentialResources)
	{
		Dictionary<string, int> resourceState = [];
		foreach (var resource in potentialResources)
			if (!resourceState.ContainsKey(resource.ResourceKey))
				resourceState[resource.ResourceKey] = resource.GetCurrentResourceAmount(state, combat);
		return resourceState;
	}

	public static (List<(string ResourceKey, bool IsSatisfied)> Payment, Dictionary<string, int> GroupedPayment, bool IsSatisfied) GetResourcePayment(Dictionary<string, int> resourceState, List<IKokoroApi.IActionCostApi.IActionCost> costs)
	{
		List<List<IKokoroApi.IActionCostApi.IResource>> toPay = [];
		foreach (var cost in costs)
			for (var i = 0; i < cost.ResourceAmount; i++)
				toPay.Add(cost.PotentialResources.ToList());

		var payment = GetBestResourcePaymentOptions(resourceState, toPay).FirstOrDefault() ?? [];
		var groupedPayment = payment.GroupBy(k => k.ResourceKey).ToDictionary(g => g.Key, g => g.Count());
		var isSatisfied = payment.All(e => e.IsSatisfied);
		return (Payment: payment, GroupedPayment: groupedPayment, IsSatisfied: isSatisfied);
	}

	private static IEnumerable<List<(string ResourceKey, bool IsSatisfied)>> GetBestResourcePaymentOptions(Dictionary<string, int> resourceState, List<List<IKokoroApi.IActionCostApi.IResource>> toPay)
		=> GetResourcePaymentOptions([], toPay)
			.Select(o =>
			{
				List<(string ResourceKey, bool IsSatisfied)> resultOption = [];
				Dictionary<string, int> currentState = new(resourceState);
				foreach (var resourceKey in o)
				{
					var isSatisfied = currentState.GetValueOrDefault(resourceKey) > 0;
					if (isSatisfied)
						currentState[resourceKey] = currentState.GetValueOrDefault(resourceKey) - 1;
					resultOption.Add((ResourceKey: resourceKey, IsSatisfied: isSatisfied));
				}
				return resultOption;
			})
			.OrderBy(o => o.Count(e => !e.IsSatisfied));

	private static IEnumerable<List<string>> GetResourcePaymentOptions(List<string> currentPayment, List<List<IKokoroApi.IActionCostApi.IResource>> toPayLeft)
	{
		if (toPayLeft.Count == 0)
		{
			yield return currentPayment;
			yield break;
		}

		var currentToPay = toPayLeft[0];
		var newToPayLeft = toPayLeft.Skip(1).ToList();

		foreach (var resource in currentToPay)
		{
			var newPayment = currentPayment.Append(resource.ResourceKey).ToList();
			foreach (var option in GetResourcePaymentOptions(newPayment, newToPayLeft))
				yield return option;
		}
	}
}

internal sealed class ActionCostImpl : IKokoroApi.IActionCostApi.IActionCost
{
	[JsonProperty]
	public IReadOnlyList<IKokoroApi.IActionCostApi.IResource> PotentialResources { get; }

	[JsonProperty]
	public int ResourceAmount { get; }

	[JsonProperty]
	public int? IconOverlap { get; }

	[JsonIgnore]
	public Spr? CostUnsatisfiedIcon { get; }

	[JsonIgnore]
	public Spr? CostSatisfiedIcon { get; }

	[JsonIgnore]
	public int? IconWidth { get; }

	[JsonIgnore]
	public IKokoroApi.IActionCostApi.CustomCostTooltipProvider? CustomTooltipProvider { get; }

	[JsonConstructor]
	public ActionCostImpl(IReadOnlyList<IKokoroApi.IActionCostApi.IResource> potentialResources, int resourceAmount, int? iconOverlap)
	{
		this.PotentialResources = potentialResources;
		this.ResourceAmount = resourceAmount;
		this.IconOverlap = iconOverlap;
	}

	public ActionCostImpl(IReadOnlyList<IKokoroApi.IActionCostApi.IResource> potentialResources, int resourceAmount, int? iconOverlap, Spr? costUnsatisfiedIcon, Spr? costSatisfiedIcon, int? iconWidth, IKokoroApi.IActionCostApi.CustomCostTooltipProvider? customTooltipProvider) : this(potentialResources, resourceAmount, iconOverlap)
	{
		this.CostUnsatisfiedIcon = costUnsatisfiedIcon;
		this.CostSatisfiedIcon = costSatisfiedIcon;
		this.IconWidth = iconWidth;
		this.CustomTooltipProvider = customTooltipProvider;
	}

	public void RenderSingle(G g, ref Vec position, IKokoroApi.IActionCostApi.IResource? satisfiedResource, bool isDisabled, bool dontRender)
	{
		if ((satisfiedResource is null ? CostUnsatisfiedIcon : CostSatisfiedIcon) is { } overriddenIcon)
		{
			if (!dontRender)
				Draw.Sprite(overriddenIcon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += IconWidth ?? 8;
		}
		else
		{
			(satisfiedResource ?? PotentialResources.FirstOrDefault())?.Render(g, ref position, isSatisfied: satisfiedResource is not null, isDisabled, dontRender);
		}
	}

	public List<Tooltip> GetTooltips(State state, Combat? combat)
	{
		if (CustomTooltipProvider is not null)
			return CustomTooltipProvider(state, combat, PotentialResources, ResourceAmount);
		return PotentialResources.FirstOrDefault()?.GetTooltips(state, combat, ResourceAmount) ?? [];
	}
}

internal sealed class ActionCostStatusResource : IKokoroApi.IActionCostApi.IResource
{
	[JsonProperty]
	public readonly Status Status;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public readonly IKokoroApi.IActionCostApi.StatusResourceTarget Target;

	[JsonIgnore]
	public string ResourceKey
		=> $"Status.{(Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player ? "Player" : "Enemy")}.{Status.Key()}";

	[JsonIgnore]
	public Spr? CostUnsatisfiedIcon { get; }

	[JsonIgnore]
	public Spr? CostSatisfiedIcon { get; }

	[JsonIgnore]
	public int? IconWidth { get; }

	[JsonConstructor]
	public ActionCostStatusResource(Status status, IKokoroApi.IActionCostApi.StatusResourceTarget target)
	{
		this.Status = status;
		this.Target = target;
	}

	public ActionCostStatusResource(Status status, IKokoroApi.IActionCostApi.StatusResourceTarget target, Spr? costUnsatisfiedIcon, Spr? costSatisfiedIcon, int? iconWidth) : this(status, target)
	{
		this.CostUnsatisfiedIcon = costUnsatisfiedIcon;
		this.CostSatisfiedIcon = costSatisfiedIcon;
		this.IconWidth = iconWidth;
	}

	public int GetCurrentResourceAmount(State state, Combat combat)
	{
		var ship = Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player ? state.ship : combat.otherShip;
		return ship.Get(Status);
	}

	public void PayResource(State state, Combat combat, int amount)
	{
		var ship = Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player ? state.ship : combat.otherShip;
		ship.Add(Status, -amount);
	}

	public void RenderPrefix(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (Target != IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow)
			return;

		if (!dontRender)
			Draw.Sprite(StableSpr.icons_outgoing, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public void Render(G g, ref Vec position, bool isSatisfied, bool isDisabled, bool dontRender)
	{
		var icon = (isSatisfied ? CostSatisfiedIcon : CostUnsatisfiedIcon) ?? DB.statuses[Status].icon;
		if (!dontRender)
			Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += IconWidth ?? 8;
	}

	public List<Tooltip> GetTooltips(State state, Combat? combat, int amount)
	{
		var nameFormat = ModEntry.Instance.Localizations.Localize(["resourceCost", "status", Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player ? "player" : "enemy", "name"]);
		var descriptionFormat = ModEntry.Instance.Localizations.Localize(["resourceCost", "status", Target == IKokoroApi.IActionCostApi.StatusResourceTarget.Player ? "player" : "enemy", "description"]);
		var icon = CostSatisfiedIcon ?? CostUnsatisfiedIcon ?? DB.statuses[Status].icon;
		var name = string.Format(nameFormat, Status.GetLocName().ToUpper());
		var description = string.Format(descriptionFormat, amount, Status.GetLocName().ToUpper());

		return [
			new GlossaryTooltip($"{typeof(ModEntry).Namespace}::ResourceCost::Status::{Status.Key()}::{Target}")
			{
				Icon = icon,
				TitleColor = Colors.action,
				Title = name,
				Description = description,
			}
		];
	}
}

internal sealed class ActionCostEnergyResource : IKokoroApi.IActionCostApi.IResource
{
	[JsonIgnore]
	public string ResourceKey
		=> "Energy";

	[JsonIgnore]
	public Spr? CostUnsatisfiedIcon
		=> (Spr)ModEntry.Instance.Content.EnergyCostUnsatisfiedSprite.Id!.Value;

	[JsonIgnore]
	public Spr? CostSatisfiedIcon
		=> (Spr)ModEntry.Instance.Content.EnergyCostSatisfiedSprite.Id!.Value;

	[JsonConstructor]
	public ActionCostEnergyResource()
	{
	}

	public int GetCurrentResourceAmount(State state, Combat combat)
		=> combat.energy;

	public void PayResource(State state, Combat combat, int amount)
		=> combat.energy -= amount;

	public void Render(G g, ref Vec position, bool isSatisfied, bool isDisabled, bool dontRender)
	{
		var icon = (isSatisfied ? CostSatisfiedIcon : CostUnsatisfiedIcon) ?? (isSatisfied ? CostUnsatisfiedIcon : CostSatisfiedIcon) ?? StableSpr.icons_energy;
		if (!dontRender)
			Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public List<Tooltip> GetTooltips(State state, Combat? combat, int amount)
		=> [
			new GlossaryTooltip($"{typeof(ModEntry).Namespace}::ResourceCost::Energy")
			{
				Icon = CostSatisfiedIcon ?? CostUnsatisfiedIcon,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["resourceCost", "energy", "name"]),
				Description = string.Format(ModEntry.Instance.Localizations.Localize(["resourceCost", "energy", "description"]), amount),
			}
		];
}