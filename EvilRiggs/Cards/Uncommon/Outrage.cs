using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon
{
	[CardMeta(deck = Deck.colorless, rarity = Rarity.uncommon, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
	internal class Outrage : Card
	{
		public override void OnDraw(State s, Combat c)
		{
			flipped = false;
		}

		public override List<CardAction> GetActions(State s, Combat c)
		{
			Status status = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
			var list = new List<CardAction>();
			switch (this.upgrade)
			{
				case Upgrade.None:
					list.Add(new ADiscard { count = 3, disabled = flipped });
					list.Add(new AEnergy { changeAmount = 1, disabled = flipped });
					list.Add(new CardActions.ASequential() { targetCard = this });
					list.Add(new AStatus() { status = status, statusAmount = 3, targetPlayer = true, disabled = !flipped });
					break;

				case Upgrade.A:
					list.Add(new ADiscard { count = 1, disabled = flipped });
					list.Add(new AEnergy { changeAmount = 1, disabled = flipped });
					list.Add(new CardActions.ASequential() { targetCard = this });
					list.Add(new AStatus() { status = status, statusAmount = 3, targetPlayer = true, disabled = !flipped });
					break;

				case Upgrade.B:
					list.Add(new ADiscard { count = 3, disabled = flipped });
					list.Add(new AEnergy { changeAmount = 1, disabled = flipped });
					list.Add(new CardActions.ASequential() { targetCard = this });
					list.Add(new AStatus() { status = status, statusAmount = 4, targetPlayer = true, disabled = !flipped });
					break;
			}

			return list;
		}

		public override CardData GetData(State state) => new CardData
		{
			cost = 1,
			floppable = false,
			infinite = true,
			art = flipped ? (Spr)Manifest.sprites["cardart_seq_offset_bottom"].Id! : (Spr)Manifest.sprites["cardart_seq_offset_top"].Id!,
			artTint = "FFFFFF"
		};

		public override string Name() => "Steam Engine";
	}
}
