using System.Diagnostics.CodeAnalysis;

namespace Shockah.Kokoro;

public sealed class ApiImplementation : IKokoroApi
{
	private static ModEntry Instance => ModEntry.Instance;

	public IEvadeHook VanillaEvadeHook
		=> Kokoro.VanillaEvadeHook.Instance;

	public IEvadeHook VanillaDebugEvadeHook
		=> Kokoro.VanillaDebugEvadeHook.Instance;

	#region Midrow
	public void TagMidrowObject(Combat combat, StuffBase @object, string tag, object? tagValue = null)
		=> MidrowTracker.ObtainMidrowTracker(combat).ObtainEntry(@object).Tags[tag] = tagValue;

	public void UntagMidrowObject(Combat combat, StuffBase @object, string tag)
		=> MidrowTracker.ObtainMidrowTracker(combat).ObtainEntry(@object).Tags.Remove(tag);

	public bool IsMidrowObjectTagged(Combat combat, StuffBase @object, string tag)
		=> MidrowTracker.ObtainMidrowTracker(combat).ObtainEntry(@object).Tags.ContainsKey(tag);

	public bool TryGetMidrowObjectTag(Combat combat, StuffBase @object, string tag, [MaybeNullWhen(false)] out object? tagValue)
		=> MidrowTracker.ObtainMidrowTracker(combat).ObtainEntry(@object).Tags.TryGetValue(tag, out tagValue);
	#endregion

	#region EvadeHook
	public void RegisterEvadeHook(IEvadeHook hook, double priority)
		=> Instance.EvadeHookManager.Register(hook, priority);

	public void UnregisterEvadeHook(IEvadeHook hook)
		=> Instance.EvadeHookManager.Unregister(hook);
	#endregion

	#region DroneShiftHook
	public IDroneShiftHook VanillaDroneShiftHook
		=> Kokoro.VanillaDroneShiftHook.Instance;

	public IDroneShiftHook VanillaDebugDroneShiftHook
		=> Kokoro.VanillaDebugDroneShiftHook.Instance;

	public void RegisterDroneShiftHook(IDroneShiftHook hook, double priority)
		=> Instance.DroneShiftHookManager.Register(hook, priority);

	public void UnregisterDroneShiftHook(IDroneShiftHook hook)
		=> Instance.DroneShiftHookManager.Unregister(hook);
	#endregion

	#region ArtifactIconHook
	public void RegisterArtifactIconHook(IArtifactIconHook hook, double priority)
		=> Instance.ArtifactIconHookManager.Register(hook, priority);

	public void UnregisterArtifactIconHook(IArtifactIconHook hook)
		=> Instance.ArtifactIconHookManager.Unregister(hook);
	#endregion
}
