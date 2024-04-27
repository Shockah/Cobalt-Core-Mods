using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class TargetLock : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			Status status = (Status)(Manifest.statuses["targetLock"].Id ?? throw new NullReferenceException());
			switch (this.upgrade)
			{

				case Upgrade.None:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 2 });
					break;

				case Upgrade.A:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 2 });
					break;

				case Upgrade.B:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 4 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			exhaust = upgrade==Upgrade.B ? true : false,
			art = StableSpr.cards_MultiBlast
		};

		public override string Name() => "Target Lock Old";
	}
}
