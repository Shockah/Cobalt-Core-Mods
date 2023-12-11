using System.Linq;

namespace Shockah.Soggins;

public sealed class APlaySpecificCardFromAnywhere : CardAction
{
	public int CardID;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		Card? card = c.hand.Concat(s.deck).Concat(c.discard).Concat(c.exhausted).FirstOrDefault(c => c.uuid == CardID);
		if (card is null)
			return;

		if (c.hand.Contains(card))
		{
			c.TryPlayCard(s, card, playNoMatterWhatForFree: true);
			return;
		}

		s.RemoveCardFromWhereverItIs(CardID);
		c.SendCardToHand(s, card);
		c.QueueImmediate(new APlaySpecificCardFromAnywhere { CardID = card.uuid });
		c.QueueImmediate(new ADelay() { time = -0.2 });
	}
}
