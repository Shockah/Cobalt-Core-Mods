using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.rare)]
internal class NoEscape : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Status status = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new AVariableHint
				{
					hand = true,
					handAmount = c.hand.Count - 1
				});
				list.Add((CardAction)new AStatus
				{
					xHint = 1,
					status = (Status)11,
					statusAmount = c.hand.Count - 1,
					targetPlayer = true
				});
				list.Add((CardAction)new AStatus
				{
					xHint = 1,
					status = (Status)23,
					statusAmount = c.hand.Count - 1,
					targetPlayer = true
				});
				break;
			case 1:
				list.Add((CardAction)new AVariableHint
				{
					hand = true,
					handAmount = c.hand.Count - 1
				});
				list.Add((CardAction)new AStatus
				{
					xHint = 1,
					status = (Status)11,
					statusAmount = c.hand.Count - 1,
					targetPlayer = true
				});
				break;
			case 2:
				list.Add((CardAction)new AVariableHint
				{
					hand = true,
					handAmount = c.hand.Count - 1
				});
				list.Add((CardAction)new AStatus
				{
					xHint = 1,
					status = (Status)11,
					statusAmount = c.hand.Count - 1,
					targetPlayer = true
				});
				list.Add((CardAction)new AStatus
				{
					xHint = 1,
					status = (Status)23,
					statusAmount = c.hand.Count - 1,
					targetPlayer = true
				});
				list.Add((CardAction)new AStatus
				{
					xHint = 1,
					status = status,
					statusAmount = c.hand.Count - 1,
					targetPlayer = true
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = (((int)base.upgrade != 2) ? 1 : 2);
		result.exhaust = (int)base.upgrade == 1;
		result.art = StableSpr.cards_HeatSink;
		return result;
	}

	public override string Name()
	{
		return "No Escape";
	}
}
