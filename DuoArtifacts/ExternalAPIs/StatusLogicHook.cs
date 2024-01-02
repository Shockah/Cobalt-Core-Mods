namespace Shockah.DuoArtifacts;

public partial interface IKokoroApi
{
	void RegisterStatusLogicHook(IStatusLogicHook hook, double priority);
	void UnregisterStatusLogicHook(IStatusLogicHook hook);
}

public interface IStatusLogicHook
{
	int ModifyStatusChange(State state, Combat combat, Ship ship, Status status, int oldAmount, int newAmount) => newAmount;
	bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status) => null;
	void OnStatusTurnTrigger(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount) { }
	bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy) => false;
}

public enum StatusTurnTriggerTiming
{
	TurnStart, TurnEnd
}

public enum StatusTurnAutoStepSetStrategy
{
	Direct, QueueSet, QueueAdd, QueueImmediateSet, QueueImmediateAdd
}