using System.Collections.Generic;

namespace EvilRiggs.Cards.Common
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Skedaddle : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new AMove() { dir = -1, targetPlayer = true, isRandom = true });
					list.Add(new AStatus() { targetPlayer = true, status = Status.hermes, statusAmount = 1 });
					break;

				case Upgrade.A:
					list.Add(new AMove() { dir = -2, targetPlayer = true, isRandom = true });
					list.Add(new AStatus() { targetPlayer = true, status = Status.hermes, statusAmount = 1 });
					break;

				case Upgrade.B:
					list.Add(new AStatus() { targetPlayer = true, status = Status.evade, statusAmount = 1 });
					list.Add(new AStatus() { targetPlayer = true, status = Status.hermes, statusAmount = 1 });
					list.Add(new AAddCard() { card = new TrashFumes(), destination = CardDestination.Deck, amount = 1 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 1,
			infinite = true,
			art = StableSpr.cards_Dodge
		};

		public override string Name() => "Skedaddle";
	}
}
