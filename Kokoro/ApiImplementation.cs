using CobaltCoreModding.Definitions.ExternalItems;
using System;
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

	#region MidrowTags
	public void TagMidrowObject(Combat combat, StuffBase @object, string tag, object? tagValue = null)
		=> MidrowTracker.ObtainMidrowTracker(combat).ObtainEntry(@object).Tags[tag] = tagValue;

	public void UntagMidrowObject(Combat combat, StuffBase @object, string tag)
		=> MidrowTracker.ObtainMidrowTracker(combat).ObtainEntry(@object).Tags.Remove(tag);

	public bool IsMidrowObjectTagged(Combat combat, StuffBase @object, string tag)
		=> MidrowTracker.ObtainMidrowTracker(combat).ObtainEntry(@object).Tags.ContainsKey(tag);

	public bool TryGetMidrowObjectTag(Combat combat, StuffBase @object, string tag, [MaybeNullWhen(false)] out object? tagValue)
		=> MidrowTracker.ObtainMidrowTracker(combat).ObtainEntry(@object).Tags.TryGetValue(tag, out tagValue);
	#endregion

	#region MidrowScorching
	public Tooltip GetScorchingTooltip(int? value = null)
		=> value is null
			? new CustomTTGlossary(CustomTTGlossary.GlossaryType.midrow, () => StableSpr.icons_overheat, () => I18n.ScorchingGlossaryName, () => I18n.ScorchingGlossaryAltDescription)
			: new CustomTTGlossary(CustomTTGlossary.GlossaryType.midrow, () => StableSpr.icons_overheat, () => I18n.ScorchingGlossaryName, () => I18n.ScorchingGlossaryDescription, new Func<object>[] { () => value.Value });

	public int GetScorchingStatus(Combat combat, StuffBase @object)
		=> TryGetMidrowObjectTag(combat, @object, ModEntry.ScorchingTag, out var value) && value is int intValue ? intValue : 0;

	public void SetScorchingStatus(Combat combat, StuffBase @object, int value)
	{
		int oldValue = GetScorchingStatus(combat, @object);
		TagMidrowObject(combat, @object, ModEntry.ScorchingTag, value);
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
