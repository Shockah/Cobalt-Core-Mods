using FSPRO;

namespace Shockah.DuoArtifacts;

internal sealed class AExhaustWherever : CardAction
{
	private const double DelayTimer = 0.3;

	public int uuid;

	public override void Begin(G g, State s, Combat c)
	{
		timer = 0.0;
		Card? card = s.FindCard(uuid);
		if (card is null || c.exhausted.Contains(card))
			return;

		card.ExhaustFX();
		Audio.Play(Event.CardHandling);
		s.deck.Remove(card);
		c.hand.Remove(card);
		c.discard.Remove(card);
		c.SendCardToExhaust(s, card);
		timer = DelayTimer;
	}
}