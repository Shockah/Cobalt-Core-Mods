using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.rare, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Strangelove : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			Status status = StatusMeta.deckToMissingStatus[(Deck)Manifest.EvilRiggsDeck.Id!];
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new AStatus() { status = status, statusAmount = 99, targetPlayer = true });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = -1 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = 0 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = 1 });
					break;

				case Upgrade.A:
					list.Add(new AStatus() { status = status, statusAmount = 5, targetPlayer = true });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = -1 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = 0 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = 1 });
					break;

				case Upgrade.B:
					list.Add(new AStatus() { status = status, statusAmount = 99, targetPlayer = true });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = -2 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = -1 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = 1 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, offset = 2 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 3,
			exhaust = true,
			art = StableSpr.cards_SeekerMissileCard
		};

		public override string Name() => "Strangelove";
	}
}
