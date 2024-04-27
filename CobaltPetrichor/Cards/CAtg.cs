using System.Collections.Generic;

namespace CobaltPetrichor.Cards
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { })]
	internal class CAtg : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			list.Add(new ASpawn {
			thing = new Missile
			{
				yAnimation = 0.0,
				missileType = MissileType.heavy
			} });
			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 1,
			temporary = true,
			exhaust = true,
			art = StableSpr.cards_SeekerMissileCard,
			artTint = "7F271F"
		};
	}
}
