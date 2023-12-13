using Shockah.Shared;

namespace Shockah.Kokoro;

public sealed class StatusLogicManager : HookManager<IStatusLogicHook>
{
	internal StatusLogicManager() : base()
	{
		Register(VanillaStatusLogicBoostHook.Instance, 0);
	}

	public bool IsAffectedByBoost(State state, Combat combat, Ship ship, Status status)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.IsAffectedByBoost(state, combat, ship, status);
			if (hookResult == false)
				return false;
			else if (hookResult == true)
				return true;
		}
		return true;
	}
}

public sealed class VanillaStatusLogicBoostHook : IStatusLogicHook
{
	public static VanillaStatusLogicBoostHook Instance { get; private set; } = new();

	private VanillaStatusLogicBoostHook() { }

	public bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status)
		=> status is Status.shield or Status.tempShield ? false : null;
}