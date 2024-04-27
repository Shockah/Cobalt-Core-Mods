using System.Collections.Generic;

namespace EvilRiggs.Cards.Common
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Jostle : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					list.Add(new ADroneMove() { dir = 2, isRandom = true });
					break;

				case Upgrade.A:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.seeker } });
					list.Add(new ADroneMove() { dir = 2, isRandom = true });
					break;

				case Upgrade.B:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					list.Add(new ADroneMove() { dir = 3, isRandom = true });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 1,
			art = StableSpr.cards_SeekerMissileCard
		};

		public override string Name() => "Jostle";
	}
}
