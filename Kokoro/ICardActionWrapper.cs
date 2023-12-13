using System.Collections.Generic;

namespace Shockah.Kokoro;

public interface ICardActionWrapper
{
	IEnumerable<CardAction> GetWrappedCardActions();
}

public static class CardActionExt
{
	public static IEnumerable<CardAction> GetWrappedCardActionsRecursively(this CardAction action)
	{
		if (action is ICardActionWrapper wrapper)
		{
			foreach (var wrappedAction in wrapper.GetWrappedCardActions())
				foreach (var recursiveWrappedAction in wrappedAction.GetWrappedCardActionsRecursively())
					yield return recursiveWrappedAction;
		}
		else
		{
			yield return action;
		}
	}
}