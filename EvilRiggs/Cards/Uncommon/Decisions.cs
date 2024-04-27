using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Decisions : Card
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new AAttack() { damage = 1, disabled = flipped });
					list.Add(new ADrawCard() { count = 1, disabled = flipped });
					list.Add(new ADummyAction());
					list.Add(new AAttack() { damage = 3, disabled = !flipped });
					list.Add(new ADiscard() { count = 1, disabled = !flipped });
					break;

				case Upgrade.A:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal } });
					list.Add(new ADrawCard() { count = 2 });
					break;

				case Upgrade.B:
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy } });
					list.Add(new ADiscard() { count = 1 });
					list.Add(new ADrawCard() { count = 2 });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 1,
			floppable = true,
			art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top
		};

		public override string Name() => "Decisions";
	}
}
