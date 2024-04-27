using System.Collections.Generic;

namespace EvilRiggs.Artifacts
{
	internal class TemperedRage : Artifact
	{
		public override void OnTurnStart(State state, Combat combat)
		{
			if (state.ship.Get((Status)Manifest.statuses["rage"].Id!) >= 4)
			{
				combat.QueueImmediate(new ADrawCard
				{
					count = 2,
					artifactPulse = Key()
				});
			}
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			return StatusMeta.GetTooltips((Status)Manifest.statuses["rage"].Id!, 4);
		}
	}
}
