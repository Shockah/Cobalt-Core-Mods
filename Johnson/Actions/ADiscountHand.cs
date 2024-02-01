using FSPRO;
using System.Collections.Generic;

namespace Shockah.Johnson;

public sealed class ADiscountHand : DynamicWidthCardAction
{
	public required int Amount;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		foreach (var card in c.hand)
			card.discount += Amount;
		Audio.Play(Event.CardHandling);
	}

	public override Icon? GetIcon(State s)
		=> new(ModEntry.Instance.DiscountHandIcon.Sprite, Amount == -1 ? null : Amount, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => ModEntry.Instance.DiscountHandIcon.Sprite,
				() => ModEntry.Instance.Localizations.Localize(["action", "DiscountHand", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["action", "DiscountHand", "description"], new { Discount = -Amount }),
				key: $"{ModEntry.Instance.Package.Manifest.UniqueName}::DiscountHand"
			)
		];
}
