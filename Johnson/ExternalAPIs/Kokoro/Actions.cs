using System.Collections.Generic;

namespace Shockah.Johnson;

public partial interface IKokoroApi
{
	IActionApi Actions { get; }

	public partial interface IActionApi
	{
		ACardOffering WithDestination(ACardOffering action, CardDestination? destination, bool? insertRandomly = null);
		CardReward WithDestination(CardReward route, CardDestination? destination, bool? insertRandomly = null);

		List<CardAction> GetWrappedCardActions(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions);
	}
}