using CobaltCoreModding.Definitions.ExternalItems;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Shockah.Kokoro;

public sealed class ApiImplementation : IKokoroApi
{
	private static ModEntry Instance => ModEntry.Instance;

	public IEvadeHook VanillaEvadeHook
		=> Kokoro.VanillaEvadeHook.Instance;

	public IEvadeHook VanillaDebugEvadeHook
		=> Kokoro.VanillaDebugEvadeHook.Instance;

	#region Generic
	public TimeSpan TotalGameTime
		=> Instance.TotalGameTime;
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

	public Tooltip GetOxidationStatusTooltip(Ship ship, State state)
		=> new TTGlossary($"status.{Instance.Content.OxidationStatus.Id!.Value}", Instance.OxidationStatusManager.GetOxidationStatusMaxValue(ship, state));

	public int GetOxidationStatusMaxValue(Ship ship, State state)
		=> Instance.OxidationStatusManager.GetOxidationStatusMaxValue(ship, state);

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

	public int GetScorchingStatus(Combat combat, StuffBase @object)
		=> TryGetExtensionData(@object, ModEntry.ScorchingTag, out int value) ? value : 0;

	public void SetScorchingStatus(Combat combat, StuffBase @object, int value)
	{
		int oldValue = GetScorchingStatus(combat, @object);
		SetExtensionData(@object, ModEntry.ScorchingTag, value);
		foreach (var hook in Instance.MidrowScorchingManager)
			hook.OnScorchingChange(combat, @object, oldValue, value);
	}

	public void AddScorchingStatus(Combat combat, StuffBase @object, int value)
		=> SetScorchingStatus(combat, @object, Math.Max(GetScorchingStatus(combat, @object) + value, 0));

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
}
