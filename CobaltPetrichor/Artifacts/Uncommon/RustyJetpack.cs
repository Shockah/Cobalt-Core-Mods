using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Uncommon
{
	internal class RustyJetpack : Artifact
	{
		public override void OnTurnStart(State state, Combat combat)
		{
			if (combat.turn != 1 && state.ship.Get(Status.evade) > 0)
			{
				combat.QueueImmediate(new AMove { targetPlayer = true, dir = -1, isRandom = true });
				combat.QueueImmediate(new AStatus { status = Status.evade, targetPlayer = true, statusAmount = -1, artifactPulse = Key() });
			}

			if (combat.turn == 1)
			{
				combat.Queue(new AStatus { status = Status.evade, targetPlayer = true, statusAmount = 4, artifactPulse = Key() });
			}
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.evade", "4"));
			return list;
		}
	}
}
