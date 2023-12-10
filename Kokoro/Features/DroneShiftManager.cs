using FSPRO;
using Shockah.Shared;

namespace Shockah.Kokoro;

public sealed class DroneShiftManager : HookManager<IDroneShiftHook>
{
	internal DroneShiftManager() : base()
	{
		Register(VanillaDroneShiftHook.Instance, 0);
		Register(VanillaDebugDroneShiftHook.Instance, int.MaxValue);
		Register(VanillaMidrowCheckDroneShiftHook.Instance, int.MaxValue);
	}

	public bool IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> GetHandlingHook(state, combat, context) is not null;

	public IDroneShiftHook? GetHandlingHook(State state, Combat combat, DroneShiftHookContext context = DroneShiftHookContext.Action)
	{
		foreach (var hook in GetHooksWithProxies(state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.IsDroneShiftPossible(state, combat, context);
			if (hookResult == false)
				return null;
			else if (hookResult == true)
				return hook;
		}
		return null;
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

		Audio.Play(Event.Status_PowerDown);
		return false;
	}

	public void PayForDroneShift(State state, Combat combat, int direction)
		=> state.ship.Add(Status.droneShift, -1);
}