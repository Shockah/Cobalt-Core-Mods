using FSPRO;
using Shockah.Shared;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class DroneShiftManager : HookManager<IDroneShiftHook>
{
	internal DroneShiftManager() : base()
	{
		Register(VanillaDroneShiftHook.Instance, 0);
		Register(VanillaDebugDroneShiftHook.Instance, 1_000_000_000);
		Register(VanillaMidrowCheckDroneShiftHook.Instance, 2_000_000_000);
	}

	public bool IsDroneShiftPossible(State state, Combat combat, int direction, DroneShiftHookContext context)
		=> GetHandlingHook(state, combat, direction, context) is not null;

	public IDroneShiftHook? GetHandlingHook(State state, Combat combat, int direction, DroneShiftHookContext context = DroneShiftHookContext.Action)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.IsDroneShiftPossible(state, combat, direction, context);
			if (hookResult == false)
				return null;
			else if (hookResult == true)
				return hook;
		}
		return null;
	}

	public void AfterDroneShift(State state, Combat combat, int direction, IDroneShiftHook hook)
	{
		foreach (var hooks in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
			hooks.AfterDroneShift(state, combat, direction, hook);
	}
}

public sealed class VanillaDroneShiftHook : IDroneShiftHook
{
	public static VanillaDroneShiftHook Instance { get; private set; } = new();

	private VanillaDroneShiftHook() { }

	public bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> state.ship.Get(Status.droneShift) > 0 ? true : null;

	public void PayForDroneShift(State state, Combat combat, int direction)
		=> state.ship.Add(Status.droneShift, -1);
}

public sealed class VanillaDebugDroneShiftHook : IDroneShiftHook
{
	public static VanillaDebugDroneShiftHook Instance { get; private set; } = new();

	private VanillaDebugDroneShiftHook() { }

	public bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> FeatureFlags.Debug && Input.shift ? true : null;
}

public sealed class VanillaMidrowCheckDroneShiftHook : IDroneShiftHook
{
	public static VanillaMidrowCheckDroneShiftHook Instance { get; private set; } = new();

	private VanillaMidrowCheckDroneShiftHook() { }

	public bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
	{
		if (context != DroneShiftHookContext.Action)
			return null;
		if (combat.stuff.Count != 0)
			return null;
		if (combat.stuff.Any(s => !s.Value.Immovable()))
			return null;

		Audio.Play(Event.Status_PowerDown);
		return false;
	}
}