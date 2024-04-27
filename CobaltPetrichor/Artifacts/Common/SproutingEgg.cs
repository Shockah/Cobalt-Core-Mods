using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Common
{
	internal class SproutingEgg : Artifact
	{
		bool triggered;

		public override void OnCombatStart(State state, Combat combat) { triggered = false; }

		public override void OnCombatEnd(State state) { triggered = false; }

		public override void OnPlayerTakeNormalDamage(State state, Combat combat, int rawAmount, Part? part) { triggered = true;  }

		public override void OnTurnStart(State state, Combat combat)
		{
			if(!triggered) { 
				combat.QueueImmediate(new AStatus
				{
					artifactPulse = Key(),
					status = Status.shield,
					statusAmount = 1,
					targetPlayer = true
				});
			}
		}

		public override Spr GetSprite()
		{
			string spr = triggered ? "sproutingEggUsed" : "sproutingEgg";
			return (Spr)Manifest.sprites[spr].Id!;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.shield", "1"));
			return list;
		}
	}
}
