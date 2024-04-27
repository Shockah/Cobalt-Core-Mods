using CobaltPetrichor.CardActions;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace CobaltPetrichor.Artifacts
{
	[ArtifactMeta(unremovable = true)]
	internal class EnemyScaling : Artifact
	{
		public int turns;
		public int difficulty;
		public int buffHull;
		public int buffDrones;
		public int buffArmored;
		public int buffPowerdrive;

		public override void OnReceiveArtifact(State state)
		{
			turns = 0;
			difficulty = 0;
			buffHull = 0;
			buffDrones = 0;
			buffArmored = 0;
			buffPowerdrive = 0;
		}

		public override int? GetDisplayNumber(State s)
		{
			return turns;
		}

		public override Spr GetSprite()
		{
			string spr = difficulty > 8 ? "rainstorm_8" : "rainstorm_"+difficulty;
			return (Spr)Manifest.sprites[spr].Id!;
		}

		public override void OnCombatStart(State state, Combat combat)
		{
			if(buffHull > 0)
			{
				combat.Queue(new AHullMax { amount = buffHull, targetPlayer = false });
				combat.Queue(new AHeal { healAmount = buffHull, targetPlayer = false });
			}

			for(int i=0; i<buffDrones; i++)
			{
				combat.Queue(new ASummon { thing = new AttackDrone {targetPlayer = true} });
			}

			if (buffArmored > 0)
			{
				var nonEmptyPartIndexes = Enumerable.Range(0, combat.otherShip.parts.Count)
					.Where(i => combat.otherShip.parts[i].type != PType.empty)
					.Shuffle(state.rngActions)
					.ToList();

				for (int i = 0; i < buffArmored; i++)
				{
					combat.Queue(new AArmor { worldX = nonEmptyPartIndexes[i] + combat.otherShip.x });
				}
			}

			if (buffPowerdrive > 0)
			{
				combat.Queue(new AStatus { status = Status.powerdrive, statusAmount= buffPowerdrive, targetPlayer = false });
			}
		}

		public override void OnTurnEnd(State state, Combat combat)
		{
			turns++;
			if(turns >= 10)
			{
				turns = 0;
				difficulty++;
				Pulse();
				switch(difficulty)
				{
					case 1: buffHull = 1; break;
					case 2: buffDrones = 1; break;
					case 3: buffHull = 4; break;
					case 4: buffArmored = 2; break;
					case 5: buffDrones = 3; break;
					case 6: buffPowerdrive = 1; break;
					case 7: buffHull = 9; break;
					case 8: buffPowerdrive = 3; break;
				}
			}
		}

		public override List<Tooltip>? GetExtraTooltips()
		{
			List<Tooltip> list = new List<Tooltip>();
			string gloss = difficulty > 8 ? "difficulty8" : "difficulty" + difficulty;
			list.Add(new TTGlossary(Manifest.glossary[gloss].Head));
			return list;
		}
	}
}
