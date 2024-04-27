using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.rare, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class EngineRedirect : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			Status status = (Status)(Manifest.statuses["engineRedirect"].Id ?? throw new NullReferenceException());
			switch (this.upgrade)
			{

				case Upgrade.None:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 3 });
					break;

				case Upgrade.A:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 3 });
					list.Add(new AStatus() { targetPlayer = true, status = Status.evade, statusAmount = 2 });
					break;

				case Upgrade.B:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 3 });
					list.Add(new AStatus() { targetPlayer = true, status = Status.drawLessNextTurn, statusAmount = 3 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = upgrade == Upgrade.B ? 0 : 3,
			exhaust = true,
			art = StableSpr.cards_Ace
		};

		public override string Name() => "Engine Redirect";
	}
}
