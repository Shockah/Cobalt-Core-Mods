using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Common
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Bide : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			Status status = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
			switch (this.upgrade)
			{
				
				case Upgrade.None:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 3 });
					break;

				case Upgrade.A:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 4 });
					break;

				case Upgrade.B:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 3 });
					list.Add(new AAttack() { damage = 1 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 1,
			art = StableSpr.cards_Heatwave
		};

		public override string Name() => "Bide";
	}
}
