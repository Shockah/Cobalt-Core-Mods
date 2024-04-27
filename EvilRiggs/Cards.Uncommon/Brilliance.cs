using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.uncommon)]
internal class Brilliance : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new ADiscard());
				list.Add((CardAction)new ADrawCard
				{
					count = 2
				});
				list.Add((CardAction)new AEnergy
				{
					changeAmount = 1
				});
				break;
			case 1:
				list.Add((CardAction)new ADiscard());
				list.Add((CardAction)new ADrawCard
				{
					count = 3
				});
				list.Add((CardAction)new AEnergy
				{
					changeAmount = 1
				});
				break;
			case 2:
				list.Add((CardAction)new ADiscard());
				list.Add((CardAction)new ADrawCard
				{
					count = 1
				});
				list.Add((CardAction)new AEnergy
				{
					changeAmount = 1
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 0;
		result.exhaust = (int)base.upgrade != 2;
		result.art = StableSpr.cards_Options;
		return result;
	}

	public override string Name()
	{
		return "Brilliance";
	}
}
