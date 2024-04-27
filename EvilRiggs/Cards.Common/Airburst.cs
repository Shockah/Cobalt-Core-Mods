using System.Collections.Generic;

namespace EvilRiggs.Cards.Common;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.common)]
internal class Airburst : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new ADroneMove
				{
					dir = 3,
					isRandom = true
				});
				break;
			case 1:
				list.Add((CardAction)new ADroneMove
				{
					dir = 3,
					isRandom = true
				});
				break;
			case 2:
				list.Add((CardAction)new ADroneMove
				{
					dir = 3,
					isRandom = true
				});
				list.Add((CardAction)new AMove
				{
					dir = 2,
					isRandom = true,
					targetPlayer = true
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 0;
		result.retain = (int)base.upgrade == 1;
		result.art = StableSpr.cards_Dodge;
		return result;
	}

	public override string Name()
	{
		return "Airburst";
	}
}
