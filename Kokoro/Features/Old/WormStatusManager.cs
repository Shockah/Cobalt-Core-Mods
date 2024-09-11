using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class WormStatusManager : IStatusLogicHook
{
	private static ModEntry Instance => ModEntry.Instance;

	internal IEnumerable<Tooltip> ModifyCardTooltips(IEnumerable<Tooltip> tooltips)
	{
		foreach (var tooltip in tooltips)
		{
			if (tooltip is TTGlossary glossary && glossary.key == $"status.{Instance.Content.WormStatus.Id!.Value}" && (glossary.vals is null || glossary.vals.Length == 0 || Equals(glossary.vals[0], "<c=boldPink>0</c>")))
				glossary.vals = ["<c=boldPink>1</c>"];
			yield return tooltip;
		}
	}

	public void OnStatusTurnTrigger(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount)
	{
		if (status != (Status)Instance.Content.WormStatus.Id!.Value || timing != StatusTurnTriggerTiming.TurnStart)
			return;

		var otherShip = ship.isPlayerShip ? combat.otherShip : state.ship;
		var wormAmount = otherShip.Get((Status)Instance.Content.WormStatus.Id!.Value);
		if (wormAmount <= 0)
			return;

		if (!otherShip.isPlayerShip)
		{
			var partXsWithIntent = Enumerable.Range(0, otherShip.parts.Count)
				.Where(x => otherShip.parts[x].intent is not null)
				.Select(x => x + otherShip.x)
				.ToList();

			foreach (var partXWithIntent in partXsWithIntent.Shuffle(state.rngActions).Take(wormAmount))
				combat.Queue(new AStunPart { worldX = partXWithIntent });
		}

		combat.Queue(new AStatus
		{
			targetPlayer = otherShip.isPlayerShip,
			status = (Status)Instance.Content.WormStatus.Id!.Value,
			statusAmount = -1
		});
	}
}
