using System.Collections.Generic;

namespace EvilRiggs.Cards.Common
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class JammedBarrel : SequentialCard
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new ASpawn { thing = new Asteroid { yAnimation = 0.0 }, disabled = SequenceInitiated });
					list.Add(new CardActions.ASequential() { CardId = uuid });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal }, disabled = !SequenceInitiated });
					break;

				case Upgrade.A:
					list.Add(new ASpawn { thing = new Asteroid { yAnimation = 0.0, bubbleShield = true }, disabled = SequenceInitiated });
					list.Add(new CardActions.ASequential() { CardId = uuid });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.normal }, disabled = !SequenceInitiated });
					break;

				case Upgrade.B:
					list.Add(new ASpawn { thing = new Asteroid { yAnimation = 0.0 }, disabled = SequenceInitiated });
					list.Add(new CardActions.ASequential() { CardId = uuid });
					list.Add(new ASpawn { thing = new Missile { yAnimation = 0.0, missileType = MissileType.heavy }, disabled = !SequenceInitiated });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 1,
			floppable = false,
			infinite = true,
			art = SequenceInitiated ? (Spr)Manifest.sprites["cardart_seq_normal_bottom"].Id! : (Spr)Manifest.sprites["cardart_seq_normal_top"].Id!,
			artTint = "FFFFFF"
		};

		public override string Name() => "Jammed Barrel";
	}
}
