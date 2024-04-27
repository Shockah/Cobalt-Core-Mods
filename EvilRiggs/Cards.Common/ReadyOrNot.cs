using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Common;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.common)]
internal class ReadyOrNot : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		Status statusDiscount = (Status)(Manifest.statuses["discountNextTurn"].Id ?? throw new NullReferenceException());
		Status statusRage = (Status)(Manifest.statuses["rage"].Id ?? throw new NullReferenceException());
		List<CardAction> list = new List<CardAction>();
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new AStatus
				{
					status = (Status)17,
					statusAmount = 1,
					targetPlayer = true
				});
				list.Add((CardAction)new AStatus
				{
					status = statusDiscount,
					statusAmount = 1,
					targetPlayer = true
				});
				break;
			case 1:
				list.Add((CardAction)new AStatus
				{
					status = (Status)17,
					statusAmount = 1,
					targetPlayer = true
				});
				list.Add((CardAction)new AStatus
				{
					status = (Status)19,
					statusAmount = 1,
					targetPlayer = true
				});
				break;
			case 2:
				list.Add((CardAction)new AStatus
				{
					status = (Status)17,
					statusAmount = 1,
					targetPlayer = true
				});
				list.Add((CardAction)new AStatus
				{
					status = statusDiscount,
					statusAmount = 1,
					targetPlayer = true
				});
				list.Add((CardAction)new AStatus
				{
					status = statusRage,
					statusAmount = 1,
					targetPlayer = true
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 1;
		result.infinite = true;
		result.art = StableSpr.cards_Prepare;
		return result;
	}

	public override string Name()
	{
		return "Swerve";
	}
}
