using System.Collections.Generic;

namespace EvilRiggs.Cards.Common;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.common)]
internal class Swerve : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new AMove
				{
					dir = -7,
					targetPlayer = true,
					isRandom = true
				});
				break;
			case 1:
				list.Add((CardAction)new AMove
				{
					dir = -7,
					targetPlayer = true,
					isRandom = true
				});
				break;
			case 2:
				list.Add((CardAction)new AMove
				{
					dir = -7,
					targetPlayer = true,
					isRandom = true
				});
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = (Status)11,
					statusAmount = 2
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
		result.art = StableSpr.cards_Dodge;
		return result;
	}

	public override string Name()
	{
		return "Swerve";
	}
}
