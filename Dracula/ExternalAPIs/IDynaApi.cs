using Nickel;

namespace Shockah.Dracula;

public interface IDynaApi
{
	IDeckEntry DynaDeck { get; }
}

public interface IDynaHook
{
	void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX) { }
}