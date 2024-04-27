using System.Collections.Generic;

namespace CobaltPetrichor
{
	internal class MapUES : MapNodeContents
	{
		public override void Render(G g, Vec v)
		{
			Draw.Sprite((Spr)Manifest.sprites["mapUES"].Id!, v.x, v.y);
		}

		public override List<Tooltip> GetTooltips(G g)
		{
			return new List<Tooltip> { new TTText("Wrecked remains from a UES ship. The cargo hold contains unique items.") };
		}

		public override Route MakeRoute(State s)
		{
			
			return new RorArtifactReward
			{
				uses = 3
			};
			//return Dialogue.MakeDialogueRouteOrSkip(s, DB.story.QuickLookup(s, ".shopBefore"), OnDone.map);
		}
	}
}
