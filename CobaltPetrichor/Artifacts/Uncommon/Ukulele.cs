using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Uncommon
{
	internal class Ukulele : Artifact
	{
		public int count;

		public override void OnPlayerAttack(State state, Combat combat)
		{
			int num = (state.ship.parts.FindIndex((Part p) => p.type == PType.cannon && p.active));
			if (num != -1)
			{
				count++;
				if (count >= 7)
				{
					count = 0;
					combat.Queue(new CardActions.AUkulele
					{
						target = num - 1,
						artifactPulse = Key()
					});
					combat.Queue(new CardActions.AUkulele
					{
						target = num+1,
						artifactPulse = Key()
					});
				}
			}
		}

		public override int? GetDisplayNumber(State s)
		{
			return count;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("action.stun"));
			return list;
		}
	}
}
