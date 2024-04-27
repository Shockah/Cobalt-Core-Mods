using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.rare)]
internal class DoABarrelRoll : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Status status = (Status)(Manifest.statuses["barrelRoll"].Id ?? throw new NullReferenceException());
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
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -2,
					isRandom = true
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
		result.cost = 2;
		result.exhaust = true;
		result.art = StableSpr.cards_Ace;
		return result;
	}

	public override string Name()
	{
		return "Do A Barrel Roll";
	}
}
