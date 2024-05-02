using Nickel;

namespace Shockah.Bloch;

public sealed class ApiImplementation : IBlochApi
{
	public IDeckEntry BlochDeck
		=> ModEntry.Instance.BlochDeck;

	public void RegisterHook(IBlochHook hook, double priority)
		=> ModEntry.Instance.HookManager.Register(hook, priority);

	public void UnregisterHook(IBlochHook hook)
		=> ModEntry.Instance.HookManager.Unregister(hook);
}
