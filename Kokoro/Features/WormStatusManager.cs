using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class WormStatusManager
{
	private static ModEntry Instance => ModEntry.Instance;

	internal void OnPlayerTurnStart(State state, Combat combat)
	{
		int worm = combat.otherShip.Get((Status)Instance.Content.WormStatus.Id!.Value);
		if (worm <= 0)
			return;

		var partXsWithIntent = Enumerable.Range(0, combat.otherShip.parts.Count)
			.Where(x => combat.otherShip.parts[x].intent is not null)
			.Select(x => x + combat.otherShip.x)
			.ToList();

		foreach (var partXWithIntent in partXsWithIntent.Shuffle(state.rngActions).Take(worm))
			combat.Queue(new AStunPart { worldX = partXWithIntent });

		if (combat.otherShip.Get(Status.timeStop) <= 0)
			return;
		combat.otherShip.Add((Status)Instance.Content.WormStatus.Id!.Value, -1);
	}

	internal IEnumerable<Tooltip> ModifyCardTooltips(IEnumerable<Tooltip> tooltips)
	{
		foreach (var tooltip in tooltips)
		{
			if (tooltip is TTGlossary glossary && glossary.key == $"status.{Instance.Content.WormStatus.Id!.Value}" && (glossary.vals is null || glossary.vals.Length == 0 || Equals(glossary.vals[0], "<c=boldPink>0</c>")))
				glossary.vals = new object[] { "<c=boldPink>1</c>" };
			yield return tooltip;
		}
	}
}
