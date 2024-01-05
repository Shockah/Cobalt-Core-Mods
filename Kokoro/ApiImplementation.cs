using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModManifests;
using Nanoray.Pintail;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

public sealed class ApiImplementation : IKokoroApi, IProxyProvider
{
	private static ModEntry Instance => ModEntry.Instance;

	private readonly IManifest Manifest;
	private readonly Dictionary<Type, ConditionalWeakTable<object, object?>> ProxyCache = new();

	public ApiImplementation(IManifest manifest)
	{
		this.Manifest = manifest;
	}

	public IEvadeHook VanillaEvadeHook
		=> Kokoro.VanillaEvadeHook.Instance;

	public IEvadeHook VanillaDebugEvadeHook
		=> Kokoro.VanillaDebugEvadeHook.Instance;

	#region Generic
	public TimeSpan TotalGameTime
		=> Instance.TotalGameTime;

	public IEnumerable<Card> GetCardsEverywhere(State state, bool hand = true, bool drawPile = true, bool discardPile = true, bool exhaustPile = true)
	{
		if (drawPile)
			foreach (var card in state.deck)
				yield return card;
		if (state.route is Combat combat)
		{
			if (hand)
				foreach (var card in combat.hand)
					yield return card;
			if (discardPile)
				foreach (var card in combat.discard)
					yield return card;
			if (exhaustPile)
				foreach (var card in combat.exhausted)
					yield return card;
		}
	}

	public bool TryProxy<T>(object @object, [MaybeNullWhen(false)] out T proxy) where T : class
	{
		if (!typeof(T).IsInterface)
		{
			proxy = null;
			return false;
		}
		if (!ProxyCache.TryGetValue(typeof(T), out var table))
		{
			table = new();
			ProxyCache[typeof(T)] = table;
		}
		if (table.TryGetValue(@object, out var rawProxy))
		{
			proxy = rawProxy is null ? default : (T)rawProxy;
			return rawProxy is not null;
		}

		var newNullableProxy = Instance.ProxyManager.TryProxy<string, T>(@object, "Unknown", Instance.Name, out var newProxy) ? newProxy : null;
		table.AddOrUpdate(@object, newNullableProxy);
		proxy = newNullableProxy is null ? default : newNullableProxy;
		return newNullableProxy is not null;
	}

	#endregion

	#region ExtensionData
	public void RegisterTypeForExtensionData(Type type)
		=> Instance.ExtensionDataManager.RegisterTypeForExtensionData(type);

	public T GetExtensionData<T>(object o, string key)
		=> Instance.ExtensionDataManager.GetExtensionData<T>(Manifest, o, key);

	public bool TryGetExtensionData<T>(object o, string key, [MaybeNullWhen(false)] out T data)
		=> Instance.ExtensionDataManager.TryGetExtensionData(Manifest, o, key, out data);

	public T ObtainExtensionData<T>(object o, string key, Func<T> factory)
		=> Instance.ExtensionDataManager.ObtainExtensionData(Manifest, o, key, factory);

	public T ObtainExtensionData<T>(object o, string key) where T : new()
		=> Instance.ExtensionDataManager.ObtainExtensionData<T>(Manifest, o, key);

	public bool ContainsExtensionData(object o, string key)
		=> Instance.ExtensionDataManager.ContainsExtensionData(Manifest, o, key);

	public void SetExtensionData<T>(object o, string key, T data)
		=> Instance.ExtensionDataManager.SetExtensionData(Manifest, o, key, data);

	public void RemoveExtensionData(object o, string key)
		=> Instance.ExtensionDataManager.RemoveExtensionData(Manifest, o, key);
	#endregion

	#region WormStatus
	public ExternalStatus WormStatus
		=> Instance.Content.WormStatus;

	public Tooltip GetWormStatusTooltip(int? value = null)
		=> value is null
			? new CustomTTGlossary(CustomTTGlossary.GlossaryType.status, () => (Spr)Instance.Content.WormSprite.Id!.Value, () => I18n.WormStatusName, () => I18n.WormStatusAltGlossaryDescription)
			: new TTGlossary($"status.{Instance.Content.WormStatus.Id!.Value}", value.Value);
	#endregion

	#region OxidationStatus
	public ExternalStatus OxidationStatus
		=> Instance.Content.OxidationStatus;

	public Tooltip GetOxidationStatusTooltip(State state, Ship ship)
		=> new TTGlossary($"status.{Instance.Content.OxidationStatus.Id!.Value}", Instance.OxidationStatusManager.GetOxidationStatusMaxValue(state, ship));

	public int GetOxidationStatusMaxValue(State state, Ship ship)
		=> Instance.OxidationStatusManager.GetOxidationStatusMaxValue(state, ship);

	public void RegisterOxidationStatusHook(IOxidationStatusHook hook, double priority)
		=> Instance.OxidationStatusManager.Register(hook, priority);

	public void UnregisterOxidationStatusHook(IOxidationStatusHook hook)
		=> Instance.OxidationStatusManager.Unregister(hook);
	#endregion

	#region MidrowScorching
	public Tooltip GetScorchingTooltip(int? value = null)
		=> value is null
			? new CustomTTGlossary(CustomTTGlossary.GlossaryType.midrow, () => StableSpr.icons_overheat, () => I18n.ScorchingGlossaryName, () => I18n.ScorchingGlossaryAltDescription)
			: new CustomTTGlossary(CustomTTGlossary.GlossaryType.midrow, () => StableSpr.icons_overheat, () => I18n.ScorchingGlossaryName, () => I18n.ScorchingGlossaryDescription, new Func<object>[] { () => value.Value });

	public int GetScorchingStatus(State state, Combat combat, StuffBase @object)
		=> TryGetExtensionData(@object, ModEntry.ScorchingTag, out int value) ? value : 0;

	public void SetScorchingStatus(State state, Combat combat, StuffBase @object, int value)
	{
		int oldValue = GetScorchingStatus(state, combat, @object);
		SetExtensionData(@object, ModEntry.ScorchingTag, value);
		foreach (var hook in Instance.MidrowScorchingManager.GetHooksWithProxies(this, state.EnumerateAllArtifacts()))
			hook.OnScorchingChange(combat, @object, oldValue, value);
	}

	public void AddScorchingStatus(State state, Combat combat, StuffBase @object, int value)
		=> SetScorchingStatus(state, combat, @object, Math.Max(GetScorchingStatus(state, combat, @object) + value, 0));

	public void RegisterMidrowScorchingHook(IMidrowScorchingHook hook, double priority)
		=> Instance.MidrowScorchingManager.Register(hook, priority);

	public void UnregisterMidrowScorchingHook(IMidrowScorchingHook hook)
		=> Instance.MidrowScorchingManager.Unregister(hook);
	#endregion

	#region EvadeHook
	public void RegisterEvadeHook(IEvadeHook hook, double priority)
		=> Instance.EvadeManager.Register(hook, priority);

	public void UnregisterEvadeHook(IEvadeHook hook)
		=> Instance.EvadeManager.Unregister(hook);
	#endregion

	#region DroneShiftHook
	public IDroneShiftHook VanillaDroneShiftHook
		=> Kokoro.VanillaDroneShiftHook.Instance;

	public IDroneShiftHook VanillaDebugDroneShiftHook
		=> Kokoro.VanillaDebugDroneShiftHook.Instance;

	public void RegisterDroneShiftHook(IDroneShiftHook hook, double priority)
		=> Instance.DroneShiftManager.Register(hook, priority);

	public void UnregisterDroneShiftHook(IDroneShiftHook hook)
		=> Instance.DroneShiftManager.Unregister(hook);
	#endregion

	#region ArtifactIconHook
	public void RegisterArtifactIconHook(IArtifactIconHook hook, double priority)
		=> Instance.ArtifactIconManager.Register(hook, priority);

	public void UnregisterArtifactIconHook(IArtifactIconHook hook)
		=> Instance.ArtifactIconManager.Unregister(hook);
	#endregion

	#region CardRenderHook
	public void RegisterCardRenderHook(ICardRenderHook hook, double priority)
		=> Instance.CardRenderManager.Register(hook, priority);

	public void UnregisterCardRenderHook(ICardRenderHook hook)
		=> Instance.CardRenderManager.Unregister(hook);
	#endregion

	#region StatusRenderHook
	public void RegisterStatusRenderHook(IStatusRenderHook hook, double priority)
		=> Instance.StatusRenderManager.Register(hook, priority);

	public void UnregisterStatusRenderHook(IStatusRenderHook hook)
		=> Instance.StatusRenderManager.Unregister(hook);

	public Color DefaultActiveStatusBarColor
		=> new("b2f2ff");

	public Color DefaultInactiveStatusBarColor
		=> DefaultActiveStatusBarColor.fadeAlpha(0.3);
	#endregion

	#region StatusLogicHook
	public void RegisterStatusLogicHook(IStatusLogicHook hook, double priority)
		=> Instance.StatusLogicManager.Register(hook, priority);

	public void UnregisterStatusLogicHook(IStatusLogicHook hook)
		=> Instance.StatusLogicManager.Unregister(hook);
	#endregion

	#region Actions
	public IKokoroApi.IActionApi Actions { get; } = new ActionApiImplementation();

	public sealed class ActionApiImplementation : IKokoroApi.IActionApi
	{
		public CardAction MakeExhaustEntireHandImmediate()
			=> new AExhaustEntireHandImmediate();

		public CardAction MakePlaySpecificCardFromAnywhere(int cardId, bool showTheCardIfNotInHand = true)
			=> new APlaySpecificCardFromAnywhere { CardId = cardId, ShowTheCardIfNotInHand = showTheCardIfNotInHand };

		public CardAction MakePlayRandomCardsFromAnywhere(IEnumerable<int> cardIds, int amount = 1, bool showTheCardIfNotInHand = true)
			=> new APlayRandomCardsFromAnywhere { CardIds = cardIds.ToHashSet(), Amount = amount, ShowTheCardIfNotInHand = showTheCardIfNotInHand };

		public CardAction MakeContinue(out Guid id)
		{
			id = Guid.NewGuid();
			return new AContinue { Id = id, Continue = true };
		}

		public CardAction MakeContinued(Guid id, CardAction action)
			=> new AContinued { Id = id, Continue = true, Action = action };

		public IEnumerable<CardAction> MakeContinued(Guid id, IEnumerable<CardAction> action)
			=> action.Select(a => MakeContinued(id, a));

		public CardAction MakeStop(out Guid id)
		{
			id = Guid.NewGuid();
			return new AContinue { Id = id, Continue = false };
		}

		public CardAction MakeStopped(Guid id, CardAction action)
			=> new AContinued { Id = id, Continue = false, Action = action };

		public IEnumerable<CardAction> MakeStopped(Guid id, IEnumerable<CardAction> action)
			=> action.Select(a => MakeStopped(id, a));

		public CardAction MakeHidden(CardAction action, bool showTooltips = false)
			=> new AHidden { Action = action, ShowTooltips = showTooltips };

		public AVariableHint SetTargetPlayer(AVariableHint action, bool targetPlayer)
		{
			var copy = Mutil.DeepCopy(action);
			Instance.Api.SetExtensionData(copy, "targetPlayer", targetPlayer);
			return copy;
		}

		public AVariableHint MakeEnergyX(AVariableHint? action = null, bool energy = true, int? tooltipOverride = null)
		{
			var copy = action is null ? new() : Mutil.DeepCopy(action);
			copy.status = Status.tempShield; // it doesn't matter, but it has to be *anything*
			Instance.Api.SetExtensionData(copy, "energy", energy);
			Instance.Api.SetExtensionData(copy, "energyTooltipOverride", tooltipOverride);
			return copy;
		}

		public AStatus MakeEnergy(AStatus action, bool energy = true)
		{
			var copy = Mutil.DeepCopy(action);
			copy.targetPlayer = true;
			Instance.Api.SetExtensionData(copy, "energy", energy);
			return copy;
		}

		public List<CardAction> GetWrappedCardActions(CardAction action)
			=> Instance.WrappedActionManager.GetWrappedCardActions(action).ToList();

		public List<CardAction> GetWrappedCardActionsRecursively(CardAction action)
			=> Instance.WrappedActionManager.GetWrappedCardActionsRecursively(action, includingWrapperActions: false).ToList();

		public List<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions)
			=> Instance.WrappedActionManager.GetWrappedCardActionsRecursively(action, includingWrapperActions).ToList();

		public void RegisterWrappedActionHook(IWrappedActionHook hook, double priority)
			=> Instance.WrappedActionManager.Register(hook, priority);

		public void UnregisterWrappedActionHook(IWrappedActionHook hook)
			=> Instance.WrappedActionManager.Unregister(hook);
	}
	#endregion

	#region ComplexActions
	public IKokoroApi.IConditionalActionApi ConditionalActions { get; } = new ConditionalActionApiImplementation();
	public IKokoroApi.IActionCostApi ActionCosts { get; } = new ActionCostApiImplementation();

	public sealed class ConditionalActionApiImplementation : IKokoroApi.IConditionalActionApi
	{
		public CardAction Make(IKokoroApi.IConditionalActionApi.IBoolExpression expression, CardAction action, bool fadeUnsatisfied = true)
			=> new AConditional { Expression = expression, Action = action, FadeUnsatisfied = fadeUnsatisfied };

		public IKokoroApi.IConditionalActionApi.IIntExpression Constant(int value)
			=> new ConditionalActionIntConstant(value);

		public IKokoroApi.IConditionalActionApi.IIntExpression HandConstant(int value)
			=> new ConditionalActionHandConstant(value);

		public IKokoroApi.IConditionalActionApi.IIntExpression XConstant(int value)
			=> new ConditionalActionXConstant(value);

		public IKokoroApi.IConditionalActionApi.IIntExpression ScalarMultiplier(IKokoroApi.IConditionalActionApi.IIntExpression expression, int scalar)
			=> new ConditionalActionScalarMultiplier(expression, scalar);

		public IKokoroApi.IConditionalActionApi.IBoolExpression HasStatus(Status status, bool targetPlayer = true, bool countNegative = false)
			=> new ConditionalActionHasStatusExpression(status, targetPlayer, countNegative);

		public IKokoroApi.IConditionalActionApi.IIntExpression Status(Status status, bool targetPlayer = true)
			=> new ConditionalActionStatusExpression(status, targetPlayer);

		public IKokoroApi.IConditionalActionApi.IBoolExpression Equation(
			IKokoroApi.IConditionalActionApi.IIntExpression lhs,
			IKokoroApi.IConditionalActionApi.EquationOperator @operator,
			IKokoroApi.IConditionalActionApi.IIntExpression rhs,
			IKokoroApi.IConditionalActionApi.EquationStyle style,
			bool hideOperator = false
		)
			=> new ConditionalActionEquation(lhs, @operator, rhs, style, hideOperator);
	}

	public sealed class ActionCostApiImplementation : IKokoroApi.IActionCostApi
	{
		public CardAction Make(IKokoroApi.IActionCostApi.IActionCost cost, CardAction action)
			=> new AResourceCost { Costs = new() { cost }, Action = action };

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
	#endregion
}
