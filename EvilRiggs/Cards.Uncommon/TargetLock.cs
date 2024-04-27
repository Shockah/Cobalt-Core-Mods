using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.uncommon, unreleased = true)]
internal class TargetLock : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Status status = (Status)(Manifest.statuses["targetLock"].Id ?? throw new NullReferenceException());
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
				break;
			case 1:
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = status,
					statusAmount = 2
				});
				break;
			case 2:
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = status,
					statusAmount = 4
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = (((int)base.upgrade != 1) ? 1 : 0);
		result.exhaust = (int)base.upgrade == 2;
		result.art = StableSpr.cards_MultiBlast;
		return result;
	}

	public override string Name()
	{
		return "Target Lock Old";
	}
}
