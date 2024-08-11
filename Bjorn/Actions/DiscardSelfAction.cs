using FSPRO;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Bjorn;

internal sealed class DiscardSelfAction : CardAction
{
	public required int CardId;

	public override Icon? GetIcon(State s)
		=> new Icon { path = StableSpr.icons_discardCard };

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::{GetType().Name}")
			{
				Icon = StableSpr.icons_discardCard,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "DiscardSelf", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "DiscardSelf", "description"])
			}
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		var index = c.hand.FindIndex(card => card.uuid == CardId);
		if (index == -1)
		{
			timer = 0;
			return;
		}

		var card = c.hand[index];
		c.hand.RemoveAt(index);
		card.waitBeforeMoving = 0;
		card.OnDiscard(s, c);
		c.SendCardToDiscard(s, card);
		Audio.Play(Event.CardHandling);
	}
}