using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.rare, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class NoEscape : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			Status status = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new AVariableHint { hand = true, handAmount = c.hand.Count-1 });
					list.Add(new AStatus() { xHint = 1, status = Status.evade, statusAmount = c.hand.Count - 1, targetPlayer = true });
					list.Add(new AStatus() { xHint = 1, status = Status.heat, statusAmount = c.hand.Count - 1, targetPlayer = true });
					break;

				case Upgrade.A:
					list.Add(new AVariableHint { hand = true, handAmount = c.hand.Count - 1 });
					list.Add(new AStatus() { xHint = 1, status = Status.evade, statusAmount = c.hand.Count - 1, targetPlayer = true });
					break;

				case Upgrade.B:
					list.Add(new AVariableHint { hand = true, handAmount = c.hand.Count - 1 });
					list.Add(new AStatus() { xHint = 1, status = Status.evade, statusAmount = c.hand.Count - 1, targetPlayer = true });
					list.Add(new AStatus() { xHint = 1, status = Status.heat, statusAmount = c.hand.Count - 1, targetPlayer = true });
					list.Add(new AStatus() { xHint = 1, status = status, statusAmount = c.hand.Count - 1, targetPlayer = true });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = upgrade == Upgrade.B ? 2 : 1,
			exhaust = upgrade == Upgrade.A ? true : false,
			art = StableSpr.cards_HeatSink
		};

		public override string Name() => "No Escape";
	}
}
