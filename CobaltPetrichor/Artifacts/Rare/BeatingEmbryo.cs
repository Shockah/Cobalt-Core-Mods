using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Rare
{
	internal class BeatingEmbryo : Artifact
	{
		public int count;

		public override int? GetDisplayNumber(State s) { return count; }

		public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
		{
			count++;

			if (count >= 6)
			{
				Pulse();
				combat.Queue(new AStatus { status = Status.boost, targetPlayer = true, statusAmount = 2 });
				count = 0;
			}
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.boost", "2"));
			return list;
		}
	}
}
