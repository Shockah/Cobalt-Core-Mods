using CobaltPetrichor.CardActions;

namespace CobaltPetrichor.Artifacts.Uncommon
{
	internal class ArmsRace : Artifact
	{
		public override void OnTurnEnd(State state, Combat combat)
		{
			Manifest.hasArmsRace = true;
		}

		public override void OnRemoveArtifact(State state)
		{
			Manifest.hasArmsRace = false;
		}

		public override void OnCombatStart(State state, Combat combat)
		{
			Manifest.hasArmsRace = true;
			combat.QueueImmediate(new ASummon { thing = new AttackDrone(), artifactPulse = Key() });
		}
	}
}
