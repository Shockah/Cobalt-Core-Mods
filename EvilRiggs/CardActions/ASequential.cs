using System.Collections.Generic;

namespace EvilRiggs.CardActions
{
	internal class ASequential : CardAction
	{
		public int CardId;

		public override void Begin(G g, State s, Combat c)
		{
			timer = 0.0;

			var card = s.FindCard(CardId);
			if (card is SequentialCard sequentialCard)
			{
				sequentialCard.SequenceInitiated = true;
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
