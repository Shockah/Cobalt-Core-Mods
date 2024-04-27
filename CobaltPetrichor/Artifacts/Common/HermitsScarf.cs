using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Common
{
	internal class HermitsScarf : Artifact
	{
		public int count;

		public override void OnCombatStart(State state, Combat combat) { count = 0; }
		public override void OnCombatEnd(State state) { count = 0; }
		public override void OnTurnStart(State state, Combat combat)
		{
			count++;
			if (count == 3)
			{
				combat.QueueImmediate(new AStatus
				{
					status = Status.autododgeRight,
					statusAmount = 2,
					targetPlayer = true,
					artifactPulse = Key()
				});
			}
		}

		public override int? GetDisplayNumber(State s)
		{
			return count;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.autododgeRight", "2"));
			return list;
		}
	}
}
