using FSPRO;
using Nickel;
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
		Audio.Play(Event.Status_PowerUp);
	}

	public override Icon? GetIcon(State s)
		=> new(ModEntry.Instance.StrengthenHandIcon.Sprite, Amount, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::StrengthenHand")
			{
				Icon = ModEntry.Instance.StrengthenHandIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "StrengthenHand", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "StrengthenHand", "description"], new { Damage = Amount })
			}
		];
}
