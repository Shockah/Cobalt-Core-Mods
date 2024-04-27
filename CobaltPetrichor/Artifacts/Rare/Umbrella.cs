using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Rare
{
	internal class Umbrella : Artifact
	{
		public override void OnTurnStart(State state, Combat combat)
		{
			if (combat.turn == 1)
			{
				combat.Queue(new AStatus { status = Status.perfectShield, targetPlayer = true, statusAmount = 2, artifactPulse = Key() });
				combat.Queue(new AHurt { hurtAmount = 5, targetPlayer = false });
			}
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.perfectShield", "2"));
			return list;
		}
	}
}
