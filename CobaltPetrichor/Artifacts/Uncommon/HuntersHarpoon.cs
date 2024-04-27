using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Uncommon
{
	internal class HuntersHarpoon : Artifact
	{
		public int count;

		public override int? GetDisplayNumber(State s) { return count; }

		public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
		{
			if(state.ship.Get(Status.autopilot) >  0)
			{
				combat.QueueImmediate(new AStatus { status = Status.autopilot, statusAmount = -1, targetPlayer = true, artifactPulse = Key() });
			}
			count++;

			if (count >= 10)
			{
				count = 0;
				combat.QueueImmediate(new AStatus { status = Status.autopilot, statusAmount = 4, targetPlayer = true, artifactPulse = Key() });
			}
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.autopilot", "4"));
			return list;
		}
	}
}
