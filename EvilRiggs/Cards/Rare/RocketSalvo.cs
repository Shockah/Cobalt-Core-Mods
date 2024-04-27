using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.rare, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class RocketSalvo : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					list.Add(new AAddCard() { card = new TrashFumes(), destination = CardDestination.Deck, amount = 2 });
					break;

				case Upgrade.A:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy } });
					list.Add(new AAddCard() { card = new TrashFumes(), destination = CardDestination.Deck, amount = 2 });
					break;

				case Upgrade.B:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal }, offset = -1 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal }, offset = 1 });
					list.Add(new AAddCard() { card = new TrashFumes(), destination = CardDestination.Deck, amount = 3 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 0,
			infinite = true,
			art = StableSpr.cards_SeekerMissileCard
		};

		public override string Name() => "Rocket Salvo";
	}
}
