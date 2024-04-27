using System.Collections.Generic;

namespace EvilRiggs.Artifacts
{
	internal class PerpetualSpeedDevice : Artifact
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
				combat.Queue(new AStatus { status = Status.evade, targetPlayer = true, statusAmount = 3, artifactPulse = Key() });
			}
		}

		public override Spr GetSprite()
		{
			string spr = triggered ? "artifact_perpetualSpeedDeviceUsed" : "artifact_perpetualSpeedDevice";
			return (Spr)Manifest.sprites[spr].Id!;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.evade"));
			return list;
		}
	}
}
