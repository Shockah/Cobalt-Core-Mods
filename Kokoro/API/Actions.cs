namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IActionApi Actions { get; }

	public partial interface IActionApi
	{
		CardAction MakePlaySpecificCardFromAnywhere(int cardId, bool showTheCardIfNotInHand = true);

		CardAction MakePlayRandomCardsFromAnywhere(
			Deck? deck = null,
			int amount = 1,
			bool fromHand = false, bool fromDrawPile = true, bool fromDiscardPile = false, bool fromExhaustPile = false,
			int? ignoreCardId = null, string? ignoreCardType = null
		);
	}
}