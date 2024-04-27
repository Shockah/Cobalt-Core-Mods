using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.rare, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class DoABarrelRoll : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			Status status = (Status)(Manifest.statuses["barrelRoll"].Id ?? throw new NullReferenceException());
			switch (this.upgrade)
			{

				case Upgrade.None:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 2 });
					break;

				case Upgrade.A:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 2 });
					list.Add(new AMove() { targetPlayer = true, dir = -2, isRandom = true });
					break;

				case Upgrade.B:
					list.Add(new AStatus() { targetPlayer = true, status = status, statusAmount = 4 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 2,
			exhaust = true,
			art = StableSpr.cards_Ace
		};

		public override string Name() => "Do A Barrel Roll";
	}
}
