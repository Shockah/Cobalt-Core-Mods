using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Common
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class ReadyOrNot : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			Status statusDiscount = (Status)(Manifest.statuses["discountNextTurn"].Id ?? throw new NullReferenceException());
			Status statusRage = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new AStatus() { status = Status.drawNextTurn, statusAmount = 1, targetPlayer = true });
					list.Add(new AStatus() { status = statusDiscount, statusAmount = 1, targetPlayer = true });
					break;

				case Upgrade.A:
					list.Add(new AStatus() { status = Status.drawNextTurn, statusAmount = 1, targetPlayer = true });
					list.Add(new AStatus() { status = Status.energyNextTurn, statusAmount = 1, targetPlayer = true });
					break;

				case Upgrade.B:
					list.Add(new AStatus() { status = Status.drawNextTurn, statusAmount = 1, targetPlayer = true });
					list.Add(new AStatus() { status = statusDiscount, statusAmount = 1, targetPlayer = true });
					list.Add(new AStatus() { status = statusRage, statusAmount = 1, targetPlayer = true });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 1,
			infinite = true,
			art = StableSpr.cards_Prepare
		};

		public override string Name() => "Swerve";
	}
}
