using FSPRO;
using System.Collections.Generic;
using System.Linq;

namespace EvilRiggs.CardActions
{
	internal class ATargetLock : CardAction
	{
		public override void Begin(G g, State s, Combat c)
		{
			foreach (StuffBase item in c.stuff.Values.ToList())
			{
				if (typeof(Missile) == item.GetType() || typeof(Drones.MissileLight) == item.GetType())
				{
					c.stuff.Remove(item.x);
					Missile value = new Missile
					{
						x = item.x,
						xLerped = item.xLerped,
						bubbleShield = item.bubbleShield,
						targetPlayer = item.targetPlayer,
						missileType = MissileType.seeker,
						age = item.age
					};
					c.stuff[item.x] = value;
				}
			}
			Audio.Play(Event.Drones_MissileLaunch);
			Audio.Play(Event.TogglePart);
		}

		public override List<Tooltip> GetTooltips(State s)
		{
			List<Tooltip> tooltips = new List<Tooltip>();
			TTGlossary glossary;
			glossary = new TTGlossary(Manifest.glossary["targetLock"].Head);
			tooltips.Add(glossary);

			return tooltips;
		}

		public override Icon? GetIcon(State s) { return new Icon((Spr)Manifest.sprites["evilRiggs_status_targetLock"].Id!, null, Colors.status); }
	}
}
