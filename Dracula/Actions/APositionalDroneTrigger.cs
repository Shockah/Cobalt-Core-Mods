using Newtonsoft.Json;
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
			new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => ModEntry.Instance.DroneTriggerIcon.Sprite,
				() => ModEntry.Instance.Localizations.Localize(["action", "droneTrigger", "name"]),
				() => ModEntry.Instance.Localizations.Localize(["action", "droneTrigger", "description"]),
				key: typeof(APositionalDroneTrigger).FullName ?? typeof(APositionalDroneTrigger).Name
			)
		];
	}

	public override Icon? GetIcon(State s)
		=> new(ModEntry.Instance.DroneTriggerIcon.Sprite, null, Colors.textMain);
}
