using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class SteamEngine : SequentialCard
	{
		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new AMove { targetPlayer = true, dir = -1, isRandom = true, disabled = SequenceInitiated });
					list.Add(new CardActions.ASequential() { CardId = uuid });
					list.Add(new AStatus() { status = Status.evade, statusAmount = 2, targetPlayer = true, disabled = !SequenceInitiated });
					break;

				case Upgrade.A:
					list.Add(new AMove { targetPlayer = true, dir = -2, isRandom = true, disabled = SequenceInitiated });
					list.Add(new CardActions.ASequential() { CardId = uuid });
					list.Add(new AStatus() { status = Status.evade, statusAmount = 2, targetPlayer = true, disabled = !SequenceInitiated });
					break;

				case Upgrade.B:
					list.Add(new AMove { targetPlayer = true, dir = -1, isRandom = true, disabled = SequenceInitiated });
					list.Add(new CardActions.ASequential() { CardId = uuid });
					list.Add(new AStatus() { status = Status.evade, statusAmount = 3, targetPlayer = true, disabled = !SequenceInitiated });
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

		public override string Name() => "Steam Engine";
	}
}
