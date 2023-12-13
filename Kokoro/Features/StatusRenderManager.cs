using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class StatusRenderManager : HookManager<IStatusRenderHook>
{
	private static ModEntry Instance => ModEntry.Instance;

	internal bool IsDuringShipStatusRendering = false;

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

	internal List<Tooltip> OverrideStatusTooltips(Status status, int amount, List<Tooltip> tooltips)
	{
		foreach (var hook in GetHooksWithProxies(Instance.Api, (StateExt.Instance ?? DB.fakeState).EnumerateAllArtifacts()))
			tooltips = hook.OverrideStatusTooltips(status, amount, IsDuringShipStatusRendering, tooltips);
		return tooltips;
	}
}