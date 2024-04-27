using System.Collections.Generic;
using CobaltPetrichor.CardActions;

namespace CobaltPetrichor.Artifacts.Common
{
	internal class Warbanner : Artifact
	{
		public override void OnCombatStart(State state, Combat combat)
		{
			if (state.map.GetCurrent().contents is not MapBattle battle)
				return;
			if (battle.battleType == BattleType.Elite || battle.battleType == BattleType.Boss )
			{
				combat.QueueImmediate(new AStatus { status = Status.powerdrive, targetPlayer = true, statusAmount = 1 });
				combat.QueueImmediate(new ASummon { thing = new Drones.DWarbanner(), artifactPulse = Key() });
			}
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.powerdrive", "1"));
			return list;
		}
	}
}
