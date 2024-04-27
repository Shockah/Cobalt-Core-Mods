using System.Collections.Generic;

namespace CobaltPetrichor.Artifacts.Common
{
	internal class BustlingFungus : Artifact
	{

		public override void OnTurnEnd(State state, Combat combat) 
		{ 
			if(!Manifest.moved)
			{
				combat.Queue(new AStatus { status = Status.tempShield, targetPlayer = true, statusAmount = 1, artifactPulse = Key() });
			}
			Manifest.moved = false; 
		}

		public override void OnCombatEnd(State state) { Manifest.moved = false; }

		public override void OnCombatStart(State state, Combat combat) { Manifest.moved = false; }

		public override Spr GetSprite()
		{
			string spr = Manifest.moved ? "bustlingFungusUsed" : "bustlingFungus";
			return (Spr)Manifest.sprites[spr].Id!;
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			list.Add(new TTGlossary("status.tempShield", "1"));
			return list;
		}
	}
}
