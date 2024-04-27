using System.Collections.Generic;

namespace EvilRiggs.Cards.Common
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Swerve : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new AMove() { dir = -7, targetPlayer = true, isRandom = true });
					break;

				case Upgrade.A:
					list.Add(new AMove() { dir = -7, targetPlayer = true, isRandom = true });
					break;

				case Upgrade.B:
					list.Add(new AMove() { dir = -7, targetPlayer = true, isRandom = true });
					list.Add(new AStatus() { targetPlayer = true, status = Status.evade, statusAmount = 2 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			exhaust = true,
			art = StableSpr.cards_Dodge
		};

		public override string Name() => "Swerve";
	}
}
