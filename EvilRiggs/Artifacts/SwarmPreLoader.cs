using System.Collections.Generic;

namespace EvilRiggs.Artifacts
{
	internal class SwarmPreLoader : Artifact
	{
		public override string Name()
		{
			return "SWARM PRELOADER";
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			return new List<Tooltip>
			{
				new TTCard
				{
					card = new EvilRiggsCard() {discount = -2}
				},
			};
		}
	}
}
