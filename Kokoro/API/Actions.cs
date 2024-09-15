using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IActionApi Actions { get; }

	public partial interface IActionApi
	{
		CardAction MakeExhaustEntireHandImmediate();
		CardAction MakePlaySpecificCardFromAnywhere(int cardId, bool showTheCardIfNotInHand = true);
		CardAction MakePlayRandomCardsFromAnywhere(IEnumerable<int> cardIds, int amount = 1, bool showTheCardIfNotInHand = true);

		CardAction MakeContinue(out Guid id);
		CardAction MakeContinued(Guid id, CardAction action);
		IEnumerable<CardAction> MakeContinued(Guid id, IEnumerable<CardAction> action);
		CardAction MakeStop(out Guid id);
		CardAction MakeStopped(Guid id, CardAction action);
		IEnumerable<CardAction> MakeStopped(Guid id, IEnumerable<CardAction> action);

		CardAction MakeSpoofed(CardAction renderAction, CardAction realAction);
		CardAction MakeHidden(CardAction action, bool showTooltips = false);
		AVariableHint SetTargetPlayer(AVariableHint action, bool targetPlayer);
		AVariableHint MakeEnergyX(AVariableHint? action = null, bool energy = true, int? tooltipOverride = null);
		AStatus MakeEnergy(AStatus action, bool energy = true);

		ACardOffering WithDestination(ACardOffering action, CardDestination? destination, bool? insertRandomly = null);
		CardReward WithDestination(CardReward route, CardDestination? destination, bool? insertRandomly = null);

		List<CardAction> GetWrappedCardActions(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions);

		void RegisterWrappedActionHook(IWrappedActionHook hook, double priority);
		void UnregisterWrappedActionHook(IWrappedActionHook hook);

		ACardSelect MakeCustomCardBrowse(ACardSelect action, ICustomCardBrowseSource source);
		
		CardAction MakeOnTurnEndAction(CardAction action);
	}
}

public interface IWrappedActionHook
{
	List<CardAction>? GetWrappedCardActions(CardAction action);
}

public interface ICustomCardBrowseSource
{
	List<Card> GetCards(State state, Combat? combat);
	string GetTitle(State state, Combat? combat, List<Card> cards);
}