namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	void RegisterStatusLogicHook(IStatusLogicHook hook, double priority);
	void UnregisterStatusLogicHook(IStatusLogicHook hook);
}

public interface IStatusLogicHook
{
	bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status) => null;
}