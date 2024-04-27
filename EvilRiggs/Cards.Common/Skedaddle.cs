using System.Collections.Generic;

namespace EvilRiggs.Cards.Common;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.common)]
internal class Skedaddle : Card
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
					dir = -1,
					targetPlayer = true,
					isRandom = true
				});
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = (Status)45,
					statusAmount = 1
				});
				break;
			case 1:
				list.Add((CardAction)new AMove
				{
					dir = -2,
					targetPlayer = true,
					isRandom = true
				});
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = (Status)45,
					statusAmount = 1
				});
				break;
			case 2:
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = (Status)11,
					statusAmount = 1
				});
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = (Status)45,
					statusAmount = 1
				});
				list.Add((CardAction)new AAddCard
				{
					card = (Card)new TrashFumes(),
					destination = (CardDestination)0,
					amount = 1
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
		result.art = StableSpr.cards_Dodge;
		return result;
	}

	public override string Name()
	{
		return "Skedaddle";
	}
}
