namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IDroneShiftHook VanillaDroneShiftHook { get; }
	IDroneShiftHook VanillaDebugDroneShiftHook { get; }
	void RegisterDroneShiftHook(IDroneShiftHook hook, double priority);
	void UnregisterDroneShiftHook(IDroneShiftHook hook);

	bool IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context);
	IDroneShiftHook? GetDroneShiftHandlingHook(State state, Combat combat, DroneShiftHookContext context);
	void AfterDroneShift(State state, Combat combat, int direction, IDroneShiftHook hook);
}

public enum DroneShiftHookContext
{
	Rendering, Action
}

public interface IDroneShiftHook
{
	bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context) => null;
	void PayForDroneShift(State state, Combat combat, int direction) { }
	void AfterDroneShift(State state, Combat combat, int direction, IDroneShiftHook hook) { }
}