namespace CobaltPetrichor.Artifacts.Uncommon
{
	internal class TimekeepersSecret : Artifact
	{
		bool triggered;

		public override void OnCombatStart(State state, Combat combat) { triggered = false; }

		public override void OnCombatEnd(State state) { triggered = false; }

		public override Spr GetSprite()
		{
			string spr = triggered ? "timekeepersSecretUsed" : "timekeepersSecret";
			return (Spr)Manifest.sprites[spr].Id!;
		}

		public override void OnPlayerLoseHull(State state, Combat combat, int amount)
		{
			//state.ship.hull-amount <= 3 && 
			if (!triggered)
			{
				triggered = true;
				Pulse();
				combat.QueueImmediate(new ADrawCard { count = 3 });
				combat.QueueImmediate(new AEnergy { changeAmount = 3 });
			}
		}
	}
}
