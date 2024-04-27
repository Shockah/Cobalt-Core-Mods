using System.Collections.Generic;

namespace EvilRiggs.Artifacts;

[ArtifactMeta(pools = [ArtifactPool.Common])]
internal class SpiltBoba : Artifact
{
	private int count = 0;

	public override void OnTurnStart(State state, Combat combat)
	{
		count = 0;
	}

	public override void OnPlayerTakeNormalDamage(State state, Combat combat, int rawAmount, Part? part)
	{
		count++;
		if (count == 1)
		{
			combat.QueueImmediate((CardAction)new AStatus
			{
				targetPlayer = true,
				status = (Status)Manifest.statuses["rage"].Id!.Value,
				statusAmount = 2,
				artifactPulse = ((Artifact)this).Key()
			});
			combat.QueueImmediate((CardAction)new AStatus
			{
				targetPlayer = true,
				status = Status.drawLessNextTurn,
				statusAmount = 1,
				artifactPulse = ((Artifact)this).Key()
			});
		}
	}

	public override Spr GetSprite()
	{
		if (count < 1)
		{
			return (Spr)Manifest.sprites["artifact_spiltBoba"].Id!.Value;
		}
		return (Spr)Manifest.sprites["artifact_spiltBobaUsed"].Id!.Value;
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips((Status)Manifest.statuses["rage"].Id!.Value, 2);
}
