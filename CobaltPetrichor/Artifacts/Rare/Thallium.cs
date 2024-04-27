using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Rare
{
	internal class Thallium : Artifact
	{
		public override void OnCombatStart(State state, Combat combat) {
			combat.Queue(new AStatus { status = Status.corrode, statusAmount = 2 });
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.corrode", "2"));
			return list;
		}
	}
}
