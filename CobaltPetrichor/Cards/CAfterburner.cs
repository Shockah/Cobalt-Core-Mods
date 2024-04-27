using System.Collections.Generic;

namespace CobaltPetrichor.Cards
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { })]
	internal class CAfterburner : Card
	{
		public int uses = 7;

		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			list.Add(new ADrawCard { count = 1  });
			list.Add(new CardActions.ALimited() { CardId = uuid });
			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 0,
			temporary = true,
			infinite = true,
			retain = true,
			art = StableSpr.cards_Options,
			artTint = "6B438E"
		};
	}
}
