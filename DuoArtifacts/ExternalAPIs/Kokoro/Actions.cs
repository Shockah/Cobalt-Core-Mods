using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

public partial interface IKokoroApi
{
	IActionApi Actions { get; }

	public interface IActionApi
	{
		CardAction MakeHidden(CardAction action, bool showTooltips = false);

		List<CardAction> GetWrappedCardActions(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action);
		List<CardAction> GetWrappedCardActionsRecursively(CardAction action, bool includingWrapperActions);
	}
}