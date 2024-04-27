using System.Collections.Generic;

namespace CobaltPetrichor.CardActions
{
	internal class ALimited : CardAction
	{
		public int CardId;
		public override void Begin(G g, State s, Combat c)
		{
			timer = 0.0;
			var card = s.FindCard(CardId) as Cards.CAfterburner;

			if (card != null)
			{
				card.uses -= 1;
				if (card.uses <= 1)
				{
					card.exhaustOverride = true;
				}
			}
		}

		public override List<Tooltip> GetTooltips(State s)
		{
			List<Tooltip> tooltips = new List<Tooltip>();
			TTGlossary glossary;
			glossary = new TTGlossary(Manifest.glossary["limitedHint"].Head, (s.FindCard(CardId) as Cards.CAfterburner)?.uses ?? 0);
			tooltips.Add(glossary);

			return tooltips;
		}

		public override Icon? GetIcon(State s) { return new Icon((Spr)Manifest.sprites["limited"].Id!, (s.FindCard(CardId) as Cards.CAfterburner)?.uses ?? 0, Colors.status); }
	}
}
