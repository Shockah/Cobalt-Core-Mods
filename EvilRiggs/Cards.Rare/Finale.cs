using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.rare)]
internal class Finale : Card
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
				list.Add((CardAction)new ADiscard());
				list.Add((CardAction)new AStatus
				{
					status = status,
					statusAmount = 7,
					targetPlayer = true
				});
				break;
			case 1:
				list.Add((CardAction)new ADiscard());
				list.Add((CardAction)new AStatus
				{
					status = status,
					statusAmount = 7,
					targetPlayer = true
				});
				break;
			case 2:
				list.Add((CardAction)new AStatus
				{
					status = status,
					statusAmount = 7,
					targetPlayer = true
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = (((int)base.upgrade != 1) ? 1 : 0);
		result.exhaust = true;
		result.art = (Spr)Manifest.sprites["cardart_ragedraw"].Id!.Value;
		return result;
	}

	public override string Name()
	{
		return "Finale";
	}
}
