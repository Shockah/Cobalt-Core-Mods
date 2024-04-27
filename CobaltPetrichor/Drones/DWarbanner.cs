using System.Collections.Generic;

namespace CobaltPetrichor.Drones
{
	internal class DWarbanner : StuffBase
	{
		public override double GetWiggleAmount()
		{
			return 1.0;
		}

		public override double GetWiggleRate()
		{
			return 1.0;
		}

		public override void Render(G g, Vec v)
		{
			Vec offset = GetOffset(g);
			DrawWithHilight(g, (Spr)Manifest.sprites["droneWarbanner"].Id!, v + offset, Mutil.Rand((double)x + 0.1) > 0.5);
		}

		public override List<CardAction>? GetActionsOnDestroyed(State s, Combat c, bool wasPlayer, int worldX)
		{
			return new List<CardAction> { new AStatus { status = Status.powerdrive, targetPlayer = true, statusAmount = -1 }};
		}

		public override List<Tooltip> GetTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary(Manifest.glossary["warbanner"].Head));

			if (bubbleShield)
			{
				list.Add(new TTGlossary("midrow.bubbleShield"));
			}

			return list;
		}

		public override Spr? GetIcon()
		{
			return (Spr)Manifest.sprites["miniWarbanner"].Id!;
		}
	}
}
