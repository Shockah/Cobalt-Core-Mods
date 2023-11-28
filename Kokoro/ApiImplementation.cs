namespace Shockah.Kokoro;

public sealed class ApiImplementation : IKokoroApi
{
	private static ModEntry Instance => ModEntry.Instance;

	#region EvadeHook
	public void RegisterEvadeHook(IEvadeHook hook, double priority)
		=> Instance.EvadeHookManager.Register(hook, priority);

	public void UnregisterEvadeHook(IEvadeHook hook)
		=> Instance.EvadeHookManager.Unregister(hook);
	#endregion

	#region DroneShiftHook
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
