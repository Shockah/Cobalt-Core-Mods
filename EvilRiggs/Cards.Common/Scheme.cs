using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Common;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.common)]
internal class Scheme : Card
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
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = status,
					statusAmount = 2
				});
				list.Add((CardAction)new ADrawCard
				{
					count = 2
				});
				break;
			case 1:
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = status,
					statusAmount = 3
				});
				list.Add((CardAction)new ADrawCard
				{
					count = 2
				});
				break;
			case 2:
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = status,
					statusAmount = 2
				});
				list.Add((CardAction)new ADrawCard
				{
					count = 3
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 1;
		result.art = (Spr)Manifest.sprites["cardart_ragedraw"].Id!.Value;
		return result;
	}

	public override string Name()
	{
		return "Scheme";
	}
}
