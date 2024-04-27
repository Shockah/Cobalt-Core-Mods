using System.Collections.Generic;

namespace CobaltPetrichor.Drones
{
	internal class DMeatNugget : StuffBase
	{
		private double particlesToEmit;

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
			DrawWithHilight(g, (Spr)Manifest.sprites["droneMeatNugget"].Id!, v + offset, Mutil.Rand((double)x + 0.1) > 0.5);
			particlesToEmit += g.dt * 2.0;
			while (particlesToEmit >= 1.0)
			{
				PFX.combatAdd.Add(new Particle
				{
					color = new Color(0.5, 0.1, 0.1),
					pos = new Vec(x * 16 + 1, v.y - 24.0) + offset + new Vec(7.5, 7.5) + Mutil.RandVel().normalized() * 6.0,
					vel = Mutil.RandVel() * 20.0,
					lifetime = 1.0,
					size = 2.0 + Mutil.NextRand() * 3.0,
					dragCoef = 1.0
				});
				particlesToEmit -= 1.0;
			}
		}

		public override List<CardAction>? GetActionsOnDestroyed(State s, Combat c, bool wasPlayer, int worldX)
		{
			return new List<CardAction>
			{
				new AHeal
				{
					healAmount = 1,
					targetPlayer = wasPlayer
				}
			};
		}

		public override List<Tooltip> GetTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary(Manifest.glossary["meatNugget"].Head));

			if (bubbleShield)
			{
				list.Add(new TTGlossary("midrow.bubbleShield"));
			}

			return list;
		}

		public override Spr? GetIcon()
		{
			return (Spr)Manifest.sprites["miniMeatNugget"].Id!;
		}
	}
}
