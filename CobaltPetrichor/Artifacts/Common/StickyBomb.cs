namespace CobaltPetrichor.Artifacts.Common
{
	internal class StickyBomb : Artifact
	{
		public override void OnCombatStart(State state, Combat combat)
		{
			for(int i=0; i < combat.otherShip.parts.Count; i++)
			{
				Manifest.enemyShipStickies[i] = false;
			}
			Manifest.enemyShipStickies[1] = true;
		}
	}
}
