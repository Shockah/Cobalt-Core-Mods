using CobaltPetrichor.CardActions;

namespace CobaltPetrichor.Artifacts.Common
{
	internal class MeatNugget : Artifact
	{
		public override void OnCombatStart(State state, Combat combat)
		{
			combat.QueueImmediate(new ASummon { thing = new Drones.DMeatNugget(), maxRadius=3, artifactPulse = Key() });
		}
	}
}
