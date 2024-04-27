using System.Collections.Generic;

namespace EvilRiggs.CardActions
{
	internal class ASequential : CardAction
	{
		public Card? targetCard;
		public override void Begin(G g, State s, Combat c)
		{
			timer = 0.0;
			if (targetCard != null)
			{
				targetCard.flipped = true;
			}
		}

		public override List<Tooltip> GetTooltips(State s)
		{
			List<Tooltip> tooltips = new List<Tooltip>();
			TTGlossary glossary;
			glossary = new TTGlossary(Manifest.glossary["sequentialHint"].Head);
			tooltips.Add(glossary);

			return tooltips;
		}
	}
}
