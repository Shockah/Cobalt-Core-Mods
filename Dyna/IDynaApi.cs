using Nickel;

namespace Shockah.Dyna;

public interface IDynaApi
{
	IStatusEntry TempNitroStatus { get; }
	IStatusEntry NitroStatus { get; }

	int GetBlastwaveDamage(Card? card, State state, int baseDamage, bool targetPlayer = false, int blastwaveIndex = 0);

	void RegisterHook(IDynaHook hook, double priority);
	void UnregisterHook(IDynaHook hook);
}

public interface IDynaHook
{
	void OnBlastwaveTrigger(State state, Combat combat, Ship ship, int worldX) { }
	void OnBlastwaveHit(State state, Combat combat, Ship ship, int originWorldX, int waveWorldX) { }
	int ModifyBlastwaveDamage(Card? card, State state, bool targetPlayer, int blastwaveIndex) => 0;

	void OnChargeSticked(State state, Combat combat, Ship ship, int worldX) { }
	void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX) { }
}