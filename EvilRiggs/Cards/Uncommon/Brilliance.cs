using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Brilliance : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new ADiscard());
					list.Add(new ADrawCard() { count = 2 });
					list.Add(new AEnergy() { changeAmount = 1 });
					break;

				case Upgrade.A:
					list.Add(new ADiscard());
					list.Add(new ADrawCard() { count = 3 });
					list.Add(new AEnergy() { changeAmount = 1 });
					break;

				case Upgrade.B:
					list.Add(new ADiscard());
					list.Add(new ADrawCard() { count = 1 });
					list.Add(new AEnergy() { changeAmount = 1 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 0,
			exhaust = upgrade==Upgrade.B ? false : true,
			art = StableSpr.cards_Options
		};

		public override string Name() => "Brilliance";
	}
}
