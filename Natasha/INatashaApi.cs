using Nickel;
using System;
using System.Collections.Generic;

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
	CardAction MakeLimitedUsesVariableHintAction(int cardId);
	CardAction MakeChangeLimitedUsesAction(int cardId, int amount, AStatusMode mode = AStatusMode.Add);
	ACardSelect SetFilterLimited(ACardSelect action, bool? limited);

	CardAction MakeOneLinerAction(List<CardAction> actions, int spacing = 3);

	void RegisterHook(INatashaHook hook, double priority);
	void UnregisterHook(INatashaHook hook);

	void RegisterManInTheMiddleStaticObject(ManInTheMiddleStaticObjectEntry entry);

	public record ManInTheMiddleStaticObjectEntry(
		string UniqueName,
		Func<State, StuffBase> Factory,
		double InitialWeight = 1,
		Func<State, double, double>? WeightProvider = null
	);
}

public interface INatashaHook
{
	bool ModifyLimitedUses(State state, Card card, int baseUses, ref int uses) => false;
}