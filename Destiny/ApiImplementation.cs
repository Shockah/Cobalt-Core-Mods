namespace Shockah.Destiny;

public sealed class ApiImplementation : IDestinyApi
{
	public void RegisterHook(IDestinyApi.IHook hook, double priority = 0)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IDestinyApi.IHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
}