using System.Collections.Generic;

namespace Shockah.Dracula;

public sealed class ABloodTap : CardAction
{
	public List<List<CardAction>>? Choices;
	public List<Status>? Statuses;

	public override Route? BeginWithRoute(G g, State s, Combat c)
		=> new ActionChoiceRoute
		{
			Title = ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "ui", "title"]),
			Choices = Choices ?? ModEntry.Instance.BloodTapManager.MakeChoices(s, c, includeEnemy: false)
		};

	public override List<Tooltip> GetTooltips(State s)
	{
		Statuses ??= [];
		List<Tooltip> tooltips = [];

		tooltips.Add(new TTText(ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "tooltip", "title"])));
		if (Statuses.Count == 0)
		{
			tooltips.Add(new TTText(ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "tooltip", "none"])));
		}
		else
		{
			foreach (var status in Statuses)
				tooltips.Add(new CustomTTGlossary(
					CustomTTGlossary.GlossaryType.status,
					() => DB.statuses[status].icon,
					() => status.GetLocName(),
					() => ""
				));
		}

		return tooltips;
	}
}
