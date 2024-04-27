using System;
using System.Collections.Generic;
using EvilRiggs.CardActions;

namespace EvilRiggs.Cards.Uncommon;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.uncommon)]
internal class Outrage : Card
{
	public override void OnDraw(State s, Combat c)
	{
		base.flipped = false;
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		Status status = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
		List<CardAction> list = new List<CardAction>();
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new ADiscard
				{
					count = 3,
					disabled = base.flipped
				});
				list.Add((CardAction)new AEnergy
				{
					changeAmount = 1,
					disabled = base.flipped
				});
				list.Add((CardAction)(object)new ASequential
				{
					targetCard = (Card?)(object)this
				});
				list.Add((CardAction)new AStatus
				{
					status = status,
					statusAmount = 3,
					targetPlayer = true,
					disabled = !base.flipped
				});
				break;
			case 1:
				list.Add((CardAction)new ADiscard
				{
					count = 1,
					disabled = base.flipped
				});
				list.Add((CardAction)new AEnergy
				{
					changeAmount = 1,
					disabled = base.flipped
				});
				list.Add((CardAction)(object)new ASequential
				{
					targetCard = (Card?)(object)this
				});
				list.Add((CardAction)new AStatus
				{
					status = status,
					statusAmount = 3,
					targetPlayer = true,
					disabled = !base.flipped
				});
				break;
			case 2:
				list.Add((CardAction)new ADiscard
				{
					count = 3,
					disabled = base.flipped
				});
				list.Add((CardAction)new AEnergy
				{
					changeAmount = 1,
					disabled = base.flipped
				});
				list.Add((CardAction)(object)new ASequential
				{
					targetCard = (Card?)(object)this
				});
				list.Add((CardAction)new AStatus
				{
					status = status,
					statusAmount = 4,
					targetPlayer = true,
					disabled = !base.flipped
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 1;
		result.floppable = false;
		result.infinite = true;
		result.art = (Spr)(base.flipped ? Manifest.sprites["cardart_seq_offset_bottom"].Id!.Value : Manifest.sprites["cardart_seq_offset_top"].Id!.Value);
		result.artTint = "FFFFFF";
		return result;
	}

	public override string Name()
	{
		return "Steam Engine";
	}
}
