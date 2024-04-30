using FSPRO;
using System.Collections.Generic;

namespace Shockah.Bloch;

internal sealed class ExhaustCardAction : CardAction
{
	public required int CardId;

	public override Icon? GetIcon(State s)
		=> new(StableSpr.icons_exhaust, null, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [new TTGlossary("cardtrait.exhaust")];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (s.FindCard(CardId) is not { } card)
			return;

		s.RemoveCardFromWhereverItIs(CardId);
		c.SendCardToExhaust(s, card);
		Audio.Play(Event.CardHandling);
	}
}