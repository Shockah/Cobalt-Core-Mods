using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.uncommon)]
internal class AllTheButtons : Card
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
				list.Add((CardAction)new AAttack
				{
					damage = 3
				});
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = status,
					statusAmount = 3
				});
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -3,
					isRandom = true
				});
				break;
			case 1:
				list.Add((CardAction)new AAttack
				{
					damage = 3
				});
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = status,
					statusAmount = 4
				});
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -4,
					isRandom = true
				});
				break;
			case 2:
				list.Add((CardAction)new AAttack
				{
					damage = 5
				});
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = status,
					statusAmount = 3
				});
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -3,
					isRandom = true
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 3;
		result.art = StableSpr.cards_riggs;
		return result;
	}

	public override string Name()
	{
		return "All The Buttons";
	}
}
