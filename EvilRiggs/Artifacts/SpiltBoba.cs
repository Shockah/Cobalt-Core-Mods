using System.Collections.Generic;

namespace EvilRiggs.Artifacts
{
	internal class SpiltBoba : Artifact
	{
		int count = 0;

		public override void OnTurnStart(State state, Combat combat)
		{
			count = 0;
		}

		public override void OnPlayerTakeNormalDamage(State state, Combat combat, int rawAmount, Part? part)
		{
			count++;
			if(count == 1)
			{
				combat.QueueImmediate(new AStatus { targetPlayer = true, status = (Status)Manifest.statuses["rage"].Id!, statusAmount = 2, artifactPulse = Key() });
				combat.QueueImmediate(new AStatus { targetPlayer = true, status = Status.drawLessNextTurn, statusAmount = 1, artifactPulse = Key() });
			}
		}

		public override Spr GetSprite()
		{
			if (count < 1)
			{
				return (Spr)Manifest.sprites["artifact_spiltBoba"].Id!;
			}

			return (Spr)Manifest.sprites["artifact_spiltBobaUsed"].Id!;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			return StatusMeta.GetTooltips((Status)Manifest.statuses["rage"].Id!, 2);
		}
	}
}
