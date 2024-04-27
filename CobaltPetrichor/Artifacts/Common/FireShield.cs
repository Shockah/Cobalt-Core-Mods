namespace CobaltPetrichor.Artifacts.Common
{
	[ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.EventOnly })]
	internal class FireShield : Artifact
	{
		int damage = 0;
		bool triggered = false;

		public override void OnTurnStart(State state, Combat combat) { damage = 0; triggered = false; }

		public override void OnCombatEnd(State state) { damage = 0; triggered = false; }

		public override int? GetDisplayNumber(State s) { return damage; }

		public override Spr GetSprite()
		{
			string spr = damage>3 ? "fireShieldUsed" : "fireShield";
			return (Spr)Manifest.sprites[spr].Id!;
		}

		public override void OnPlayerLoseHull(State state, Combat combat, int amount)
		{
			damage += amount;
			if(damage >= 2 && !triggered) {
				triggered = true;
				Pulse();
				combat.QueueImmediate(new AAttack { damage = 5 });
			}
		}
	}
}
