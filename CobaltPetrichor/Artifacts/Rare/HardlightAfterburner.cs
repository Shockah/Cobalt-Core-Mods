using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Rare
{
	internal class HardlightAfterburner : Artifact
	{
		public override void OnCombatStart(State state, Combat combat)
		{
			combat.QueueImmediate(new AAddCard
			{
				card = new Cards.CAfterburner(),
				destination = CardDestination.Hand,
				artifactPulse = Key()
			});
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			return new List<Tooltip>
			{
				new TTCard
				{
					card = new Cards.CAfterburner()
				}
			};
		}
	}
}
