using System.Collections.Generic;

namespace EvilRiggs.Artifacts;

[ArtifactMeta(pools = [ArtifactPool.Common])]
internal class SwarmPreLoader : Artifact
{
	public override string Name()
	{
		return "SWARM PRELOADER";
	}

	public override List<Tooltip>? GetExtraTooltips()
	{
		return new List<Tooltip> { (Tooltip)new TTCard
		{
			card = (Card)new EvilRiggsCard
			{
				discount = -2
			}
		} };
	}
}
