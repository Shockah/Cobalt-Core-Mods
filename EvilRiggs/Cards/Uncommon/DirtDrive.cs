using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class DirtDrive : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new AStatus() { status = Status.evade, statusAmount = 2, targetPlayer = true });
					list.Add(new AAddCard() { card = new ColorlessTrash(), destination = CardDestination.Deck, amount = 1 });
					break;

				case Upgrade.A:
					list.Add(new AStatus() { status = Status.evade, statusAmount = 2, targetPlayer = true });
					list.Add(new AAddCard() { card = new ColorlessTrash(), destination = CardDestination.Deck, amount = 1 });
					break;

				case Upgrade.B:
					list.Add(new AStatus() { status = Status.evade, statusAmount = 4, targetPlayer = true });
					list.Add(new AAddCard() { card = new ColorlessTrash(), destination = CardDestination.Deck, amount = 1 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = upgrade == Upgrade.B ? 2 : 1,
			infinite = upgrade == Upgrade.A ? true : false,
			art = StableSpr.cards_Dodge
		};

		public override string Name() => "Dirt Drive";
	}
}
