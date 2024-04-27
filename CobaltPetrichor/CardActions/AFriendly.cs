using System.Collections.Generic;

namespace CobaltPetrichor.CardActions
{
	internal class AFriendly : CardAction
	{
		public int CardId;
		public override void Begin(G g, State s, Combat c)
		{
			var card = s.FindCard(CardId) as Cards.CStrangeCreature;

			if (card != null)
			{
				int statnumber = (s.rngActions.NextInt() % 2) + 1;
				List<Status> stats = new List<Status> { Status.overdrive, Status.evade, Status.shield };
				statnumber += stats.IndexOf(card.status);
				statnumber = statnumber % 3;
				card.status = stats[statnumber];
			}
		}

		public override List<Tooltip> GetTooltips(State s)
		{
			List<Tooltip> tooltips = new List<Tooltip>();
			TTGlossary glossary;
			glossary = new TTGlossary(Manifest.glossary["friendlyHint"].Head);
			tooltips.Add(glossary);

			return tooltips;
		}

		public override Icon? GetIcon(State s) { return new Icon((Spr)Manifest.sprites["friendly"].Id!, null, Colors.status); }
	}
}