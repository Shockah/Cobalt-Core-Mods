using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModManifests;
using daisyowl.text;
using Nanoray.Pintail;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

public sealed class ApiImplementation(
	IManifest manifest
) : IKokoroApi
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly Dictionary<Type, ConditionalWeakTable<object, object?>> ProxyCache = [];

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
			table = [];
			ProxyCache[typeof(T)] = table;
		}
		if (table.TryGetValue(@object, out var rawProxy))
		{
			proxy = (T)rawProxy!;
			return rawProxy is not null;
		}

		var newNullableProxy = Instance.Helper.Utilities.ProxyManager.TryProxy<string, T>(@object, "Unknown", Instance.Name, out var newProxy) ? newProxy : null;
		table.AddOrUpdate(@object, newNullableProxy);
		proxy = newNullableProxy;
		return newNullableProxy is not null;
	}

	#endregion

	#region ExtensionData
	public void RegisterTypeForExtensionData(Type type)
		=> Instance.ExtensionDataManager.RegisterTypeForExtensionData(type);

	public T GetExtensionData<T>(object o, string key)
		=> Instance.ExtensionDataManager.GetExtensionData<T>(manifest, o, key);

	public bool TryGetExtensionData<T>(object o, string key, [MaybeNullWhen(false)] out T data)
		=> Instance.ExtensionDataManager.TryGetExtensionData(manifest, o, key, out data);

	public T ObtainExtensionData<T>(object o, string key, Func<T> factory)
		=> Instance.ExtensionDataManager.ObtainExtensionData(manifest, o, key, factory);

	public T ObtainExtensionData<T>(object o, string key) where T : new()
		=> Instance.ExtensionDataManager.ObtainExtensionData<T>(manifest, o, key);

	public bool ContainsExtensionData(object o, string key)
		=> Instance.ExtensionDataManager.ContainsExtensionData(manifest, o, key);

	public void SetExtensionData<T>(object o, string key, T data)
		=> Instance.ExtensionDataManager.SetExtensionData(manifest, o, key, data);

	public void RemoveExtensionData(object o, string key)
		=> Instance.ExtensionDataManager.RemoveExtensionData(manifest, o, key);
	#endregion

	#region WormStatus
	public ExternalStatus WormStatus
		=> Instance.Content.WormStatus;

	public Status WormVanillaStatus
		=> (Status)WormStatus.Id!.Value;

	public Tooltip GetWormStatusTooltip(int? value = null)
		=> value is null
			? new CustomTTGlossary(CustomTTGlossary.GlossaryType.status, () => (Spr)Instance.Content.WormSprite.Id!.Value, () => I18n.WormStatusName, () => I18n.WormStatusAltGlossaryDescription)
			: new TTGlossary($"status.{Instance.Content.WormStatus.Id!.Value}", value.Value);
	#endregion

	#region RedrawStatus
	public ExternalStatus RedrawStatus
		=> Instance.Content.RedrawStatus;

	public Status RedrawVanillaStatus
		=> (Status)RedrawStatus.Id!.Value;

	public Tooltip GetRedrawStatusTooltip()
		=> new TTGlossary($"status.{Instance.Content.RedrawStatus.Id!.Value}", 1);

	public void RegisterRedrawStatusHook(IRedrawStatusHook hook, double priority)
		=> Instance.RedrawStatusManager.Register(hook, priority);

	public void UnregisterRedrawStatusHook(IRedrawStatusHook hook)
		=> Instance.RedrawStatusManager.Unregister(hook);

	public bool IsRedrawPossible(State state, Combat combat, Card card)
		=> Instance.RedrawStatusManager.IsRedrawPossible(state, combat, card);

	public bool DoRedraw(State state, Combat combat, Card card)
		=> Instance.RedrawStatusManager.DoRedraw(state, combat, card);

	public IRedrawStatusHook StandardRedrawStatusPaymentHook
		=> Kokoro.StandardRedrawStatusPaymentHook.Instance;

	public IRedrawStatusHook StandardRedrawStatusActionHook
		=> Kokoro.StandardRedrawStatusActionHook.Instance;
	#endregion

	#region StatusNextTurn
	public ExternalStatus TempShieldNextTurnStatus
		=> Instance.Content.TempShieldNextTurnStatus;

	public Status TempShieldNextTurnVanillaStatus
		=> (Status)TempShieldNextTurnStatus.Id!.Value;

	public ExternalStatus ShieldNextTurnStatus
		=> Instance.Content.ShieldNextTurnStatus;

	public Status ShieldNextTurnVanillaStatus
		=> (Status)ShieldNextTurnStatus.Id!.Value;
	#endregion

	#region OxidationStatus
	public ExternalStatus OxidationStatus
		=> ExternalStatus.GetRaw((int)Instance.Content.OxidationStatus.Status);

	public Status OxidationVanillaStatus
		=> (Status)OxidationStatus.Id!.Value;

	public Tooltip GetOxidationStatusTooltip(State state, Ship ship)
		=> new TTGlossary($"status.{Instance.Content.OxidationStatus.Status}", Instance.OxidationStatusManager.GetOxidationStatusMaxValue(state, ship));

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
			: new CustomTTGlossary(CustomTTGlossary.GlossaryType.midrow, () => StableSpr.icons_overheat, () => I18n.ScorchingGlossaryName, () => I18n.ScorchingGlossaryDescription, [() => value.Value]);

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
	public IEvadeHook VanillaEvadeHook
		=> Kokoro.VanillaEvadeHook.Instance;

	public IEvadeHook VanillaDebugEvadeHook
		=> Kokoro.VanillaDebugEvadeHook.Instance;

	public void RegisterEvadeHook(IEvadeHook hook, double priority)
		=> Instance.EvadeManager.Register(hook, priority);

	public void UnregisterEvadeHook(IEvadeHook hook)
		=> Instance.EvadeManager.Unregister(hook);

	public bool IsEvadePossible(State state, Combat combat, int direction, EvadeHookContext context)
		=> Instance.EvadeManager.IsEvadePossible(state, combat, direction, context);

	public bool IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> Instance.EvadeManager.IsEvadePossible(state, combat, 0, context);

	public IEvadeHook? GetEvadeHandlingHook(State state, Combat combat, int direction, EvadeHookContext context)
		=> Instance.EvadeManager.GetHandlingHook(state, combat, direction, context);

	public IEvadeHook? GetEvadeHandlingHook(State state, Combat combat, EvadeHookContext context)
		=> Instance.EvadeManager.GetHandlingHook(state, combat, 0, context);

	public void AfterEvade(State state, Combat combat, int direction, IEvadeHook hook)
		=> Instance.EvadeManager.AfterEvade(state, combat, direction, hook);
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

	public bool IsDroneShiftPossible(State state, Combat combat, int direction, DroneShiftHookContext context)
		=> Instance.DroneShiftManager.IsDroneShiftPossible(state, combat, direction, context);

	public bool IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> Instance.DroneShiftManager.IsDroneShiftPossible(state, combat, 0, context);

	public IDroneShiftHook? GetDroneShiftHandlingHook(State state, Combat combat, int direction, DroneShiftHookContext context)
		=> Instance.DroneShiftManager.GetHandlingHook(state, combat, direction, context);

	public IDroneShiftHook? GetDroneShiftHandlingHook(State state, Combat combat, DroneShiftHookContext context)
		=> Instance.DroneShiftManager.GetHandlingHook(state, combat, 0, context);

	public void AfterDroneShift(State state, Combat combat, int direction, IDroneShiftHook hook)
		=> Instance.DroneShiftManager.AfterDroneShift(state, combat, direction, hook);
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

	public Font PinchCompactFont
		=> ModEntry.Instance.Content.PinchCompactFont;
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

		public CardAction MakeSpoofed(CardAction renderAction, CardAction realAction)
			=> new ASpoofed { RenderAction = renderAction, RealAction = realAction };

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

		public ACardOffering WithDestination(ACardOffering action, CardDestination? destination, bool? insertRandomly = null)
		{
			var copy = Mutil.DeepCopy(action);

			if (destination is null)
				Instance.Api.RemoveExtensionData(copy, "destination");
			else
				Instance.Api.SetExtensionData(copy, "destination", destination.Value);

			if (insertRandomly is null)
				Instance.Api.RemoveExtensionData(copy, "destinationInsertRandomly");
			else
				Instance.Api.SetExtensionData(copy, "destinationInsertRandomly", insertRandomly.Value);

			return copy;
		}

		public CardReward WithDestination(CardReward route, CardDestination? destination, bool? insertRandomly = null)
		{
			var copy = Mutil.DeepCopy(route);

			if (destination is null)
				Instance.Api.RemoveExtensionData(copy, "destination");
			else
				Instance.Api.SetExtensionData(copy, "destination", destination.Value);

			if (insertRandomly is null)
				Instance.Api.RemoveExtensionData(copy, "destinationInsertRandomly");
			else
				Instance.Api.SetExtensionData(copy, "destinationInsertRandomly", insertRandomly.Value);

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

		public ACardSelect MakeCustomCardBrowse(ACardSelect action, ICustomCardBrowseSource source)
			=> Instance.CustomCardBrowseManager.MakeCustomCardBrowse(action, source);
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
	#endregion
}