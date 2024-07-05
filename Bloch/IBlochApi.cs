using Newtonsoft.Json;
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
	CardAction MakeOnDiscardAction(CardAction action);
	CardAction MakeOnTurnEndAction(CardAction action);
	CardAction MakeSpontaneousAction(CardAction action);

	IMultiCardBrowseRoute MakeMultiCardBrowseRoute();
	IReadOnlyList<Card>? GetSelectedMultiCardBrowseCards(CardAction action);

	string GetChooseAuraOnPlayUISubtitle(int amount);
	string GetChooseAuraOnDiscardUISubtitle(int amount);
	string GetChooseAuraOnTurnEndUISubtitle(int amount);

	void RegisterHook(IBlochHook hook, double priority);
	void UnregisterHook(IBlochHook hook);

	public interface IMultiCardBrowseRoute
	{
		Route AsRoute { get; }

		IReadOnlyList<CustomAction>? CustomActions { get; set; }
		int MinSelected { get; set; }
		int MaxSelected { get; set; }
		bool EnabledSorting { get; set; }
		bool BrowseActionIsOnlyForTitle { get; set; }
		IReadOnlyList<Card>? CardsOverride { get; set; }

		IMultiCardBrowseRoute SetCustomActions(IReadOnlyList<CustomAction>? value);
		IMultiCardBrowseRoute SetMinSelected(int value);
		IMultiCardBrowseRoute SetMaxSelected(int value);
		IMultiCardBrowseRoute SetEnabledSorting(bool value);
		IMultiCardBrowseRoute SetBrowseActionIsOnlyForTitle(bool value);
		IMultiCardBrowseRoute SetCardsOverride(IReadOnlyList<Card>? value);

		[method: JsonConstructor]
		public record struct CustomAction(
			CardAction? Action,
			string Title,
			int MinSelected = 0,
			int MaxSelected = int.MaxValue
		);
	}
}

public interface IBlochHook
{
	void OnScryResult(State state, Combat combat, IReadOnlyList<Card> presentedCards, IReadOnlyList<Card> discardedCards, bool fromInsight) { }
}