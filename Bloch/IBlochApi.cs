using Nickel;
using System.Collections.Generic;

namespace Shockah.Bloch;

public interface IBlochApi
{
	IDeckEntry BlochDeck { get; }

	IStatusEntry VeilingAuraStatus { get; }
	IStatusEntry FeedbackAuraStatus { get; }
	IStatusEntry InsightAuraStatus { get; }
	IStatusEntry IntensifyStatus { get; }
	IStatusEntry DrawEachTurnStatus { get; }
	IStatusEntry MindMapStatus { get; }
	IStatusEntry PersonalitySplitStatus { get; }

	ICardTraitEntry SpontaneousTriggeredTrait { get; }

	CardAction MakeChooseAura(Card card, int amount, string? uiSubtitle = null, int actionId = 0);
	CardAction MakeScryAction(int amount);
	CardAction MakeSpontaneousAction(CardAction action);

	string GetChooseAuraOnPlayUISubtitle(int amount);
	string GetChooseAuraOnDiscardUISubtitle(int amount);
	string GetChooseAuraOnTurnEndUISubtitle(int amount);

	void RegisterHook(IBlochHook hook, double priority);
	void UnregisterHook(IBlochHook hook);
}

public interface IBlochHook
{
	void OnScryResult(State state, Combat combat, IReadOnlyList<Card> presentedCards, IReadOnlyList<Card> discardedCards, bool fromInsight) { }
}