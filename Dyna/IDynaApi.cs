using Nickel;

namespace Shockah.Dyna;

public interface IDynaApi
{
	IStatusEntry TempNitroStatus { get; }
	IStatusEntry NitroStatus { get; }
	IStatusEntry BastionStatus { get; }

	int GetBlastwaveDamage(Card? card, State state, int baseDamage, bool targetPlayer = false, int blastwaveIndex = 0);

	void RegisterHook(IDynaHook hook, double priority);
	void UnregisterHook(IDynaHook hook);
}

public interface IDynaHook
{
	void OnBlastwaveTrigger(State state, Combat combat, Ship ship, int worldX, bool hitMidrow) { }
	void OnBlastwaveHit(State state, Combat combat, Ship ship, int originWorldX, int waveWorldX, bool hitMidrow) { }
	int ModifyBlastwaveDamage(Card? card, State state, bool targetPlayer, int blastwaveIndex) => 0;

	void OnChargeFired(State state, Combat combat, Ship targetShip, int worldX) { }
	void OnChargeSticked(State state, Combat combat, Ship ship, int worldX) { }
	void OnChargeTrigger(State state, Combat combat, Ship ship, int worldX) { }
}