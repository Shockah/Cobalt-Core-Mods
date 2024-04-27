using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Uncommon
{
	internal class RedWhip : Artifact
	{
		bool attacked;
		public override void OnTurnEnd(State state, Combat combat)
		{
			if (!attacked)
			{
				combat.QueueImmediate(new AStatus { status = Status.evade, targetPlayer = true, statusAmount = 2, artifactPulse = Key() });
			}
			attacked = false;
		}

		public override void OnCombatStart(State state, Combat combat) { attacked = false; }

		public override void OnCombatEnd(State state) { attacked = false; }

		public override void OnPlayerAttack(State state, Combat combat) { attacked = true; }

		public override Spr GetSprite()
		{
			string spr = attacked ? "redWhipUsed" : "redWhip";
			return (Spr)Manifest.sprites[spr].Id!;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.evade", "2"));
			return list;
		}
	}
}
