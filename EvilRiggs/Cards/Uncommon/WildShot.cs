using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class WildShot : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			Status status = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
			switch (this.upgrade)
			{

				case Upgrade.None:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					list.Add(new AMove() { targetPlayer = true, dir = -2, isRandom = true });
					list.Add(new ADroneMove() { dir = 1, isRandom = true });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					break;

				case Upgrade.A:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy } });
					list.Add(new AMove() { targetPlayer = true, dir = -2, isRandom = true });
					list.Add(new ADroneMove() { dir = 1, isRandom = true });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.seeker } });
					break;

				case Upgrade.B:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					list.Add(new AMove() { targetPlayer = true, dir = -2, isRandom = true });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					list.Add(new ADroneMove() { dir = 1, isRandom = true });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 2,
			art = StableSpr.cards_SeekerMissileCard
		};

		public override string Name() => "Wild Shot";
	}
}
