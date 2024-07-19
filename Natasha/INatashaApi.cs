using Nickel;

namespace Shockah.Natasha;

public interface INatashaApi
{
	IDeckEntry NatashaDeck { get; }

	IStatusEntry ReprogrammedStatus { get; }
	IStatusEntry DeprogrammedStatus { get; }

	ICardTraitEntry LimitedTrait { get; }

	int GetBaseLimitedUses(string key, Upgrade upgrade);
	void SetBaseLimitedUses(string key, int value);
	void SetBaseLimitedUses(string key, Upgrade upgrade, int value);
	int GetStartingLimitedUses(State state, Card card);
	int GetLimitedUses(State state, Card card);
	void SetLimitedUses(State state, Card card, int value);
	void ResetLimitedUses(State state, Card card);

	void RegisterHook(INatashaHook hook, double priority);
	void UnregisterHook(INatashaHook hook);
}

public interface INatashaHook
{
	bool ModifyLimitedUses(State state, Card card, int baseUses, ref int uses) => false;
}