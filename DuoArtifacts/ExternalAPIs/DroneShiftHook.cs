namespace Shockah.DuoArtifacts;

public partial interface IKokoroApi
{
	void RegisterDroneShiftHook(IDroneShiftHook hook, double priority);
	void UnregisterDroneShiftHook(IDroneShiftHook hook);
}

public enum DroneShiftHookContext
{
	Rendering, Action
}

public interface IDroneShiftHook
{
	bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context) => null;
	void PayForDroneShift(State state, Combat combat, int direction) { }
	void AfterDroneShift(State state, Combat combat, int direction) { }
}