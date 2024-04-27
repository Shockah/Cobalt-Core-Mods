using System.Collections.Generic;

namespace CobaltPetrichor.Cards
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { })]
	internal class CStrangeCreature : Card
	{
		public Status status = Status.shield;
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			list.Add(new CardActions.AFriendly() { CardId = uuid });
			list.Add(new AStatus { status = status, statusAmount = 1, targetPlayer = true });
			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 2,
			temporary = true,
			infinite = true,
			retain = true,
			art = StableSpr.cards_colorless,
			artTint = "ffffff"
		};
	}
}
