using Shockah.Shared;

namespace Shockah.Kokoro;

public sealed class StatusRenderManager : HookManager<IStatusRenderHook>
{
	public bool ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.ShouldShowStatus(state, combat, ship, status, amount);
			if (hookResult == false)
				return false;
			else if (hookResult == true)
				return true;
		}
		return true;
	}

	public IStatusRenderHook? GetOverridingAsBarsHook(State state, Combat combat, Ship ship, Status status, int amount)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.ShouldOverrideStatusRenderingAsBars(state, combat, ship, status, amount);
			if (hookResult == false)
				return null;
			else if (hookResult == true)
				return hook;
		}
		return null;
	}
}