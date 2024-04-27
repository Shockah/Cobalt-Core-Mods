namespace CobaltPetrichor.Artifacts.Common
{
	internal class MortarTube : Artifact
	{
		public int count;

		public override void OnPlayerAttack(State state, Combat combat)
		{
			count++;
			if (count >= 10)
			{
				count = 0;
				combat.QueueImmediate(new ASpawn
				{
					thing = new Missile(),
					artifactPulse = Key()
				});
			}
		}

		public override int? GetDisplayNumber(State s)
		{
			return count;
		}
	}
}
