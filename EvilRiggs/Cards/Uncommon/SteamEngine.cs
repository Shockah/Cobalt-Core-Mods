using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class SteamEngine : Card
	{
		public override void OnDraw(State s, Combat c)
		{
			flipped = false;
		}

		public override List<CardAction> GetActions(State s, Combat c)
		{
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new AMove { targetPlayer = true, dir = -1, isRandom = true, disabled = flipped });
					list.Add(new CardActions.ASequential() { targetCard = this });
					list.Add(new AStatus() { status = Status.evade, statusAmount = 2, targetPlayer = true, disabled = !flipped });
					break;

				case Upgrade.A:
					list.Add(new AMove { targetPlayer = true, dir = -2, isRandom = true, disabled = flipped });
					list.Add(new CardActions.ASequential() { targetCard = this });
					list.Add(new AStatus() { status = Status.evade, statusAmount = 2, targetPlayer = true, disabled = !flipped });
					break;

				case Upgrade.B:
					list.Add(new AMove { targetPlayer = true, dir = -1, isRandom = true, disabled = flipped });
					list.Add(new CardActions.ASequential() { targetCard = this });
					list.Add(new AStatus() { status = Status.evade, statusAmount = 3, targetPlayer = true, disabled = !flipped });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 1,
			floppable = false,
			infinite = true,
			art = flipped ? (Spr)Manifest.sprites["cardart_seq_normal_bottom"].Id! : (Spr)Manifest.sprites["cardart_seq_normal_top"].Id!,
			artTint = "FFFFFF"
		};

		public override string Name() => "Steam Engine";
	}
}
