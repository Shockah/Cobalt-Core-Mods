using System;
using System.Collections.Generic;
using System.Linq;

namespace CobaltPetrichor.CardActions
{
	internal class ASummon : CardAction
	{
		public StuffBase thing = null!;
		public int maxRadius = 1;
		public bool aboveShip = true;
		public bool scalesUp = true;
		public bool burstFromPlayer = true;

		public List<int> Shuffle(List<int> list, State s)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = (int)Math.Floor(s.rngActions.Next() * n);
				int value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
			return list;
		}


		public override void Begin(G g, State state, Combat combat)
		{
			List<int> list = new List<int>();
			while(list.Count == 0) { 
				for (int i = state.ship.x - maxRadius; i < state.ship.x + state.ship.parts.Count() + maxRadius; i++)
				{
					if (!combat.stuff.ContainsKey(i))
					{
						list.Add(i);
					}
				}
				maxRadius++;
			}

			List<int> list2 = Shuffle(list, state);
			int pos = list2[0];
			thing.x = pos;
			thing.xLerped = pos;
			combat.stuff.Add(pos, thing);
			ParticleBursts.DroneSpawn(pos, true);
		}
	}
}
