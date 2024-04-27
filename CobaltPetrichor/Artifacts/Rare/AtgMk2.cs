using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Rare
{
	internal class AtgMk2 : Artifact
	{
		public int count;

		public override void OnPlayerAttack(State state, Combat combat)
		{
			count++;
			if (count >= 5)
			{
				count = 0;
				combat.QueueImmediate(new AAddCard
				{
					card = new Cards.CAtg(),
					destination = CardDestination.Hand,
					amount = 3,
					artifactPulse = Key()
				});
			}
		}

		public override int? GetDisplayNumber(State s)
		{
			return count;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			return new List<Tooltip>
			{
				new TTCard
				{
					card = new Cards.CAtg()
				}
			};
		}
	}
}
