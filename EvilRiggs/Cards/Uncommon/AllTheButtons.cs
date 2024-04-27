using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class AllTheButtons : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			Status status = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
			switch (this.upgrade)
			{

				case Upgrade.None:
					list.Add(new AAttack() { damage = 3 });
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 3 });
					list.Add(new AMove() { targetPlayer = true, dir = -3, isRandom = true });
					break;

				case Upgrade.A:
					list.Add(new AAttack() { damage = 3 });
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 4 });
					list.Add(new AMove() { targetPlayer = true, dir = -4, isRandom = true });
					break;

				case Upgrade.B:
					list.Add(new AAttack() { damage = 5 });
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 3 });
					list.Add(new AMove() { targetPlayer = true, dir = -3, isRandom = true });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 3,
			art = StableSpr.cards_riggs
		};

		public override string Name() => "All The Buttons";
	}
}
