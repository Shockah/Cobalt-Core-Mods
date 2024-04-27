using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.rare, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Finale : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			Status status = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new ADiscard());
					list.Add(new AStatus() { status = status, statusAmount = 7, targetPlayer = true });
					break;

				case Upgrade.A:
					list.Add(new ADiscard());
					list.Add(new AStatus() { status = status, statusAmount = 7, targetPlayer = true });
					break;

				case Upgrade.B:
					list.Add(new AStatus() { status = status, statusAmount = 7, targetPlayer = true });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			exhaust = true,
			art = (Spr)Manifest.sprites["cardart_ragedraw"].Id!
		};

		public override string Name() => "Finale";
	}
}
