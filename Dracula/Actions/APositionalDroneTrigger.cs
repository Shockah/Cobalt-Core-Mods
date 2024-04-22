using Newtonsoft.Json;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Dracula;

public sealed class APositionalDroneTrigger : CardAction
{
	[JsonProperty]
	public required int WorldX;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
		if (!c.stuff.TryGetValue(WorldX, out var @object))
			return;
		c.Queue(@object.GetActions(s, c) ?? []);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		if (s.route is Combat combat && combat.stuff.TryGetValue(WorldX, out var @object))
			@object.hilight = 2;

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
