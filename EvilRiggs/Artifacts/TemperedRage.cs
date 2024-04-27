using System.Collections.Generic;

namespace EvilRiggs.Artifacts;

[ArtifactMeta(pools = [ArtifactPool.Common])]
internal class TemperedRage : Artifact
{
	public override void OnTurnStart(State state, Combat combat)
	{
		if (state.ship.Get((Status)Manifest.statuses["rage"].Id!.Value) >= 4)
		{
			combat.QueueImmediate((CardAction)new ADrawCard
			{
				count = 2,
				artifactPulse = ((Artifact)this).Key()
			});
		}
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips((Status)Manifest.statuses["rage"].Id!.Value, 4);
}
