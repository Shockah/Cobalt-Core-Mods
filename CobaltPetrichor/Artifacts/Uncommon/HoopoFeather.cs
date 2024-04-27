using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Uncommon
{
	internal class HoopoFeather : Artifact
	{
		public override void OnTurnStart(State state, Combat combat)
		{
			if(state.ship.Get(Status.evade) <= 0)
			{
				combat.QueueImmediate(new AStatus { status = Status.hermes, statusAmount = 1, targetPlayer = true, artifactPulse = Key() });
			}
		}
		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.hermes", "1"));
			return list;
		}
	}

}
