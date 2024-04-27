using EvilRiggs.CardActions;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class TargetLockV2 : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{

				case Upgrade.None:
					list.Add(new ATargetLock());
					break;

				case Upgrade.A:
					list.Add(new ATargetLock());
					break;

				case Upgrade.B:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.seeker } });
					list.Add(new ATargetLock());
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 0,
			retain = upgrade == Upgrade.A ? true : false,
			art = StableSpr.cards_MultiBlast
		};

		public override string Name() => "Target Lock";
	}
}
