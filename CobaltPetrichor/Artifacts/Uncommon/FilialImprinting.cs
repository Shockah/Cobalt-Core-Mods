using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Uncommon
{
	internal class FilialImprinting : Artifact
	{
		public override void OnCombatStart(State state, Combat combat)
		{
			int statnumber = (state.rngActions.NextInt() % 3) + 0;
			List<Status> stats = new List<Status> { Status.overdrive, Status.evade, Status.shield };
			Status status = stats[statnumber];
			combat.QueueImmediate(new AAddCard
			{
				card = new Cards.CStrangeCreature() { status = status },
				destination = CardDestination.Hand,
				artifactPulse = Key()
			});
		}

		public override void OnTurnEnd(State state, Combat combat)
		{
			foreach(Card card in combat.hand)
			{
				if(card.GetType() == typeof(Cards.CStrangeCreature))
				{
					card.discount -= 1;
				}
			}
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			return new List<Tooltip>
			{
				new TTCard
				{
					card = new Cards.CStrangeCreature()
				}
			};
		}
	}
}
