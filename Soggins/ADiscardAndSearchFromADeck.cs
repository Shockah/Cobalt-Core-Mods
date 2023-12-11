using FSPRO;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Soggins;

public sealed class ADiscardAndSearchFromADeck : CardAction
{
	public Deck Deck = Deck.colorless;
	public int? Limit = null;
	public bool FromDrawPile = true;
	public bool FromDiscard = true;
	public int? IgnoreCardID = null;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		IEnumerable<Card> cardsToDiscardEnumerable = c.hand.Where(c => c.GetMeta().deck != Deck);
		if (Limit is not null)
			cardsToDiscardEnumerable = cardsToDiscardEnumerable
				.Shuffle(s.rngActions)
				.Take(Limit.Value);

		var cardsToDiscard = cardsToDiscardEnumerable.ToList();
		foreach (var card in cardsToDiscard)
		{
			c.hand.Remove(card);
			c.SendCardToDiscard(s, card);
		}

		IEnumerable<Card> cardsToDrawEnumerable = Enumerable.Empty<Card>();
		if (FromDrawPile)
			cardsToDrawEnumerable = cardsToDrawEnumerable.Concat(s.deck.Where(c => c.GetMeta().deck == Deck).Reverse());
		if (FromDiscard)
			cardsToDrawEnumerable = cardsToDrawEnumerable.Concat(c.discard.Where(c => c.GetMeta().deck == Deck).Shuffle(s.rngShuffle));
		if (IgnoreCardID is not null)
			cardsToDrawEnumerable = cardsToDrawEnumerable.Where(c => c.uuid != IgnoreCardID.Value);
		cardsToDrawEnumerable = cardsToDrawEnumerable.Take(cardsToDiscard.Count);

		foreach (var card in cardsToDrawEnumerable.ToList())
		{
			s.deck.Remove(card);
			c.discard.Remove(card);
			c.SendCardToHand(s, card);
		}

		Audio.Play(Event.CardHandling);
	}
}
