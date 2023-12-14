using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class APlayRandomCardsFromAnywhere : CardAction
{
	public Deck? Deck = null;
	public int Amount = 1;
	public bool FromHand = false;
	public bool FromDrawPile = true;
	public bool FromDiscardPile = false;
	public bool FromExhaustPile = false;
	public int? IgnoreCardID = null;
	public string? IgnoreCardType = null;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		IEnumerable<Card> potentialCards = Enumerable.Empty<Card>();
		if (FromHand)
			potentialCards = potentialCards.Concat(c.hand);
		if (FromDrawPile)
			potentialCards = potentialCards.Concat(s.deck);
		if (FromDiscardPile)
			potentialCards = potentialCards.Concat(c.discard);
		if (FromExhaustPile)
			potentialCards = potentialCards.Concat(c.exhausted);
		if (Deck is not null)
			potentialCards = potentialCards.Where(c => c.GetMeta().deck == Deck.Value);
		if (IgnoreCardID is not null)
			potentialCards = potentialCards.Where(c => c.uuid != IgnoreCardID.Value);
		if (IgnoreCardType is not null)
			potentialCards = potentialCards.Where(c => c.Key() != IgnoreCardType);
		potentialCards = potentialCards.Where(c => !c.GetDataWithOverrides(s).unplayable);
		potentialCards = potentialCards.Shuffle(s.rngActions);

		foreach (var card in potentialCards.Take(Amount))
			c.QueueImmediate(new APlaySpecificCardFromAnywhere { CardID = card.uuid });
	}
}
