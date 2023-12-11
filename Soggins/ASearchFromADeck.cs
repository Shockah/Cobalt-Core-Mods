using FSPRO;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Soggins;

public sealed class ASearchFromADeck : CardAction
{
	public Deck Deck = Deck.colorless;
	public int Amount = 1;
	public bool FromDrawPile = true;
	public bool FromDiscard = true;
	public int? IgnoreCardID = null;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		IEnumerable<Card> cardsToDrawEnumerable = Enumerable.Empty<Card>();
		if (FromDrawPile)
			cardsToDrawEnumerable = cardsToDrawEnumerable.Concat(s.deck.Where(c => c.GetMeta().deck == Deck).Reverse());
		if (FromDiscard)
			cardsToDrawEnumerable = cardsToDrawEnumerable.Concat(c.discard.Where(c => c.GetMeta().deck == Deck).Shuffle(s.rngShuffle));
		if (IgnoreCardID is not null)
			cardsToDrawEnumerable = cardsToDrawEnumerable.Where(c => c.uuid != IgnoreCardID.Value);
		cardsToDrawEnumerable = cardsToDrawEnumerable.Take(Amount);

		foreach (var card in cardsToDrawEnumerable.ToList())
		{
			s.deck.Remove(card);
			c.discard.Remove(card);
			c.SendCardToHand(s, card);
		}

		Audio.Play(Event.CardHandling);
	}
}
