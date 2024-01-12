using System.Collections.Generic;

namespace Shockah.Dracula;

public sealed class ABloodTap : CardAction
{
	public List<List<CardAction>>? Choices;

	public override Route? BeginWithRoute(G g, State s, Combat c)
		=> new ActionChoiceRoute
		{
			Title = ModEntry.Instance.Localizations.Localize(["card", "BloodTap", "ui", "title"]),
			Choices = Choices ?? ModEntry.Instance.BloodTapManager.MakeChoices(s, c, includeEnemy: false)
		};
}
