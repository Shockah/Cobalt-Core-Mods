using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Uncommon
{
	internal class LockedJewel : Artifact
	{
		public bool triggered;

		public override void OnCombatStart(State state, Combat combat) { triggered = false; }
		public override void OnTurnStart(State state, Combat combat) { triggered = false; }
		public override void OnCombatEnd(State state) { triggered = false; }


		public override void OnQueueEmptyDuringPlayerTurn(State state, Combat combat)
		{
			if (combat.hand.Count == 0 && !triggered)
			{
				triggered = true;
				combat.Queue(new AStatus { status = Status.shield, targetPlayer = true, statusAmount = 3, artifactPulse = Key() });
				combat.Queue(new AStatus { status = Status.tempShield, targetPlayer = true, statusAmount = 3, artifactPulse = Key() });
			}
		}

		public override Spr GetSprite()
		{
			string spr = triggered ? "lockedJewelUsed" : "lockedJewel";
			return (Spr)Manifest.sprites[spr].Id!;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.shield", "3"));
			list.Add(new TTGlossary("status.tempShield", "3"));
			return list;
		}
	}
}
