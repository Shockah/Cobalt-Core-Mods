using CobaltCoreModding.Definitions.ExternalItems;
using Nanoray.Pintail;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

public sealed class ApiImplementation : IKokoroApi, IProxyProvider
{
	private static ModEntry Instance => ModEntry.Instance;

	private readonly Dictionary<Type, ConditionalWeakTable<object, object?>> ProxyCache = new();

	public IEvadeHook VanillaEvadeHook
		=> Kokoro.VanillaEvadeHook.Instance;

	public IEvadeHook VanillaDebugEvadeHook
		=> Kokoro.VanillaDebugEvadeHook.Instance;

	#region Generic
	public TimeSpan TotalGameTime
		=> Instance.TotalGameTime;

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
	private static T ConvertExtensionData<T>(object o) where T : notnull
	{
		if (typeof(T).IsInstanceOfType(o))
			return (T)o;
		if (typeof(T) == typeof(int))
			return (T)(object)Convert.ToInt32(o);
		else if (typeof(T) == typeof(long))
			return (T)(object)Convert.ToInt64(o);
		else if (typeof(T) == typeof(short))
			return (T)(object)Convert.ToInt16(o);
		else if (typeof(T) == typeof(byte))
			return (T)(object)Convert.ToByte(o);
		else if (typeof(T) == typeof(bool))
			return (T)(object)Convert.ToBoolean(o);
		else if (typeof(T) == typeof(float))
			return (T)(object)Convert.ToSingle(o);
		else if (typeof(T) == typeof(double))
			return (T)(object)Convert.ToDouble(o);
		else
			return (T)o;
	}

	public T GetExtensionData<T>(object o, string key) where T : notnull
	{
		if (!Instance.ExtensionDataStorage.TryGetValue(o, out var extensionData))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		if (!extensionData.TryGetValue(key, out var data))
			throw new KeyNotFoundException($"Object {o} does not contain extension data with key `{key}`");
		return ConvertExtensionData<T>(data);
	}

	public bool TryGetExtensionData<T>(object o, string key, [MaybeNullWhen(false)] out T data) where T : notnull
	{
		if (!Instance.ExtensionDataStorage.TryGetValue(o, out var extensionData))
		{
			data = default;
			return false;
		}
		if (!extensionData.TryGetValue(key, out var rawData))
		{
			data = default;
			return false;
		}
		data = ConvertExtensionData<T>(rawData);
		return true;
	}

	public bool ContainsExtensionData(object o, string key)
	{
		if (!Instance.ExtensionDataStorage.TryGetValue(o, out var extensionData))
			return false;
		if (!extensionData.TryGetValue(key, out _))
			return false;
		return true;
	}

	public void SetExtensionData<T>(object o, string key, T data) where T : notnull
	{
		if (!Instance.ExtensionDataStorage.TryGetValue(o, out var extensionData))
		{
			extensionData = new();
			Instance.ExtensionDataStorage.AddOrUpdate(o, extensionData);
		}
		extensionData[key] = data;
	}

	public void RemoveExtensionData(object o, string key)
	{
		if (!Instance.ExtensionDataStorage.TryGetValue(o, out var extensionData))
			return;
		extensionData.Remove(key);
	}
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
			=> new APlaySpecificCardFromAnywhere { CardID = cardId, ShowTheCardIfNotInHand = showTheCardIfNotInHand };

		public CardAction MakePlayRandomCardsFromAnywhere(
			Deck? deck = null,
			int amount = 1,
			bool fromHand = false, bool fromDrawPile = true, bool fromDiscardPile = false, bool fromExhaustPile = false,
			int? ignoreCardId = null, string? ignoreCardType = null
		)
			=> new APlayRandomCardsFromAnywhere
			{
				Deck = deck,
				Amount = amount,
				FromHand = fromHand, FromDrawPile = fromDrawPile, FromDiscardPile = fromDiscardPile, FromExhaustPile = fromExhaustPile,
				IgnoreCardID = ignoreCardId, IgnoreCardType = ignoreCardType
			};
	}
	#endregion

	#region ComplexActions
	public IKokoroApi.IConditionalActionApi ConditionalActions { get; } = new ConditionalActionApiImplementation();

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

		public IKokoroApi.IConditionalActionApi.IBoolExpression Equation(IKokoroApi.IConditionalActionApi.IIntExpression lhs, IKokoroApi.IConditionalActionApi.EquationOperator @operator, IKokoroApi.IConditionalActionApi.IIntExpression rhs, bool hideOperator = false)
			=> new ConditionalActionEquation(lhs, @operator, rhs, hideOperator);
	}
	#endregion
}
