using System.Collections.Generic;

namespace Shockah.Dyna;

public partial interface IKokoroApi
{
	IActionApi Actions { get; }

	public interface IActionApi
	{
		List<CardAction> GetWrappedCardActions(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions);
	}
}