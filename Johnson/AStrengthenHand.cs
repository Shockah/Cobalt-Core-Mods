using System.Collections.Generic;

namespace Shockah.Johnson;

public sealed class AStrengthenHand : DynamicWidthCardAction
{
	public required int Amount;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		foreach (var card in c.hand)
			card.AddStrengthen(Amount);
	}

	public override Icon? GetIcon(State s)
		=> new(ModEntry.Instance.StrengthenHandIcon.Sprite, Amount, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => ModEntry.Instance.StrengthenHandIcon.Sprite,
				() => ModEntry.Instance.Localizations.Localize(["action", "StrengthenHand", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["action", "StrengthenHand", "description"], new { Damage = Amount }),
				key: $"{ModEntry.Instance.Package.Manifest.UniqueName}::StrengthenHand"
			)
		];
}
