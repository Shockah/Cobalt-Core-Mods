using System.Collections.Generic;
using System.Linq;
using Nickel;

namespace Shockah.Soggins;

internal sealed class ABotchesVariableHint : AVariableHint
{
	public ABotchesVariableHint()
	{
		this.hand = true;
	}

	public override Icon? GetIcon(State s)
		=> new() { path = (Spr)ModEntry.Instance.BotchesStatusSprite.Id!.Value };

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip("action.xHintBotches.desc")
			{
				Description = s.route is Combat ? I18n.BotchesVariableHintDescriptionStateful : I18n.BotchesVariableHintDescriptionStateless,
				vals = s.route is Combat ? [s.EnumerateAllArtifacts().OfType<BotchTrackerArtifact>().FirstOrDefault()?.Botches ?? 0] : null,
			}
		];
}