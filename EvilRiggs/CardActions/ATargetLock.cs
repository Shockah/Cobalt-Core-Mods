using System;
using System.Collections.Generic;
using System.Linq;
using CobaltCoreModding.Definitions.ExternalItems;
using EvilRiggs.Drones;
using FMOD;
using FSPRO;

namespace EvilRiggs.CardActions;

internal class ATargetLock : CardAction
{
	public override void Begin(G g, State s, Combat c)
	{
		foreach (StuffBase item in c.stuff.Values.ToList())
		{
			if (typeof(Missile) == ((object)item).GetType() || typeof(MissileLight) == ((object)item).GetType())
			{
				c.stuff.Remove(item.x);
				Missile value = new Missile
				{
					x = item.x,
					xLerped = item.xLerped,
					bubbleShield = item.bubbleShield,
					targetPlayer = item.targetPlayer,
					missileType = (MissileType)3,
					age = item.age
				};
				c.stuff[item.x] = (StuffBase)(object)value;
			}
		}
		Audio.Play((GUID?)Event.Drones_MissileLaunch, true);
		Audio.Play((GUID?)Event.TogglePart, true);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		List<Tooltip> tooltips = new List<Tooltip>();
		ExternalGlossary obj = Manifest.glossary["targetLock"];
		TTGlossary glossary = new TTGlossary(obj.Head, Array.Empty<object>());
		tooltips.Add((Tooltip)(object)glossary);
		return tooltips;
	}

	public override Icon? GetIcon(State s)
	{
		return new Icon((Spr)Manifest.sprites["evilRiggs_status_targetLock"].Id!.Value, (int?)null, Colors.status, false);
	}
}
