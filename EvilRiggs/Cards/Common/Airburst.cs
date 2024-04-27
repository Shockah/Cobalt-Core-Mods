using System.Collections.Generic;

namespace EvilRiggs.Cards.Common
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Airburst : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new ADroneMove() { dir = 3, isRandom = true });
					break;

				case Upgrade.A:
					list.Add(new ADroneMove() { dir = 3, isRandom = true });
					break;

				case Upgrade.B:
					list.Add(new ADroneMove() { dir = 3, isRandom = true });
					list.Add(new AMove() { dir = 2, isRandom = true, targetPlayer = true });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 0,
			retain = upgrade==Upgrade.A ? true : false,
			art = StableSpr.cards_Dodge
		};

		public override string Name() => "Airburst";
	}
}
