using Nickel;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

public sealed class ABayDroneTrigger : CardAction
{
	public required bool FromPlayer;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		var ship = FromPlayer ? s.ship : c.otherShip;
		c.QueueImmediate(
			Enumerable.Range(0, ship.parts.Count)
				.Where(i => ship.parts[i].type == PType.missiles && ship.parts[i].active)
				.Select(i => new APositionalDroneTrigger { WorldX = ship.x + i })
		);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		foreach (var i in Enumerable.Range(0, s.ship.parts.Count))
		{
			if (s.ship.parts[i].type != PType.missiles || !s.ship.parts[i].active)
				continue;
			s.ship.parts[i].hilight = true;

			if (s.route is Combat combat && combat.stuff.TryGetValue(s.ship.x + i, out var @object))
				@object.hilight = 2;
		}

		return [
			new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::Action::DroneTrigger")
			{
				Icon = ModEntry.Instance.DroneTriggerIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "droneTrigger", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "droneTrigger", "description"])
			}
		];
	}

	public override Icon? GetIcon(State s)
		=> new(ModEntry.Instance.DroneTriggerIcon.Sprite, null, Colors.textMain);
}
