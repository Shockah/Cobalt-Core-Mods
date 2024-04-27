using EvilRiggs.Drones;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Common
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class LightMissile : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new ASpawn { thing = new MissileLight { yAnimation = 0.0 } });
					//list.Add(new AStatus() { targetPlayer = true, status = Status.drawLessNextTurn, statusAmount = 1 });
					//list.Add(new ADrawCard() { count = 2 });
					list.Add(new AStatus() { targetPlayer = true, status = Status.evade, statusAmount = 1 });
					break;

				case Upgrade.A:
					//list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					//list.Add(new ADrawCard() { count = 2 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					list.Add(new AStatus() { targetPlayer = true, status = Status.evade, statusAmount = 1 });
					break;

				case Upgrade.B:
					//list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy } });
					//list.Add(new AStatus() { targetPlayer = true, status = Status.drawLessNextTurn, statusAmount = 1 });
					//list.Add(new ADrawCard() { count = 2 });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					list.Add(new AStatus() { targetPlayer = true, status = Status.evade, statusAmount = 2 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			//cost = 1,
			cost = upgrade == Upgrade.B ? 0 : 1,
			exhaust = upgrade == Upgrade.B ? true : false,
			art = StableSpr.cards_SeekerMissileCard
		};

		public override string Name() => "Light Missile";
	}
}
