using CobaltPetrichor.CardActions;

namespace CobaltPetrichor.Artifacts.Common
{
	internal class BundleOfFireworks : Artifact
	{
		public bool triggered;

		public override void OnCombatStart(State state, Combat combat) { triggered = false; }
		public override void OnCombatEnd(State state) { triggered = false; }


		public override void OnQueueEmptyDuringPlayerTurn(State state, Combat combat)
		{
			if (combat.hand.Count == 0 && !triggered)
			{
				triggered = true;
				combat.QueueImmediate(new ASummon { thing = new Missile { missileType = MissileType.seeker}, maxRadius = 2, artifactPulse = Key() });
				combat.QueueImmediate(new ASummon { thing = new Missile { missileType = MissileType.seeker }, maxRadius = 2, artifactPulse = Key() });
			}
		}

		public override Spr GetSprite()
		{
			string spr = triggered ? "bundleOfFireworksUsed" : "bundleOfFireworks";
			return (Spr)Manifest.sprites[spr].Id!;
		}
	}
}
