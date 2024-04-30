using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Common
{
	internal class ArcaneBlades : Artifact
	{
		public int count;
		public override void OnCombatStart(State state, Combat combat) { count = 0; }
		public override void OnCombatEnd(State state) { count = 0; }
		public override void OnTurnStart(State state, Combat combat)
		{
			if (state.map.GetCurrent().contents is not MapBattle battle)
				return;
			if (battle.battleType == BattleType.Elite || battle.battleType == BattleType.Boss)
			{
				count++;
				if (count % 3 == 0)
				{
					combat.QueueImmediate(new AStatus
					{
						status = Status.evade,
						statusAmount = 3,
						targetPlayer = true,
						artifactPulse = Key()
					});
				}
			}
		}

		public override int? GetDisplayNumber(State s)
		{
			return count;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.evade", "1"));
			return list;
		}
	}
}
