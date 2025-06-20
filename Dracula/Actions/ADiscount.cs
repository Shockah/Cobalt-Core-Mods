using System;
using System.Collections.Generic;
using FSPRO;

namespace Shockah.Dracula;

internal sealed class ADiscount : CardAction
{
	public required int CardId;
	public int Discount = -1;

	public override Icon? GetIcon(State s)
		=> new(Discount <= 0 ? StableSpr.icons_discount : StableSpr.icons_expensive, Math.Abs(Discount), Discount <= 0 ? Colors.textMain : Colors.downside);

	public override List<Tooltip> GetTooltips(State s)
		=> [new TTGlossary(Discount <= 0 ? "cardtrait.discount" : "cardtrait.expensive", "<c=boldPink>1</c>")];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		if (s.FindCard(CardId) is not { } card)
		{
			timer = 0;
			return;
		}

		card.discount += Discount;
		Audio.Play(Event.Status_PowerUp);
	}
}