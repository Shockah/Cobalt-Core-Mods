using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.uncommon)]
internal class DirtDrive : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new AStatus
				{
					status = (Status)11,
					statusAmount = 2,
					targetPlayer = true
				});
				list.Add((CardAction)new AAddCard
				{
					card = (Card)new ColorlessTrash(),
					destination = (CardDestination)0,
					amount = 1
				});
				break;
			case 1:
				list.Add((CardAction)new AStatus
				{
					status = (Status)11,
					statusAmount = 2,
					targetPlayer = true
				});
				list.Add((CardAction)new AAddCard
				{
					card = (Card)new ColorlessTrash(),
					destination = (CardDestination)0,
					amount = 1
				});
				break;
			case 2:
				list.Add((CardAction)new AStatus
				{
					status = (Status)11,
					statusAmount = 4,
					targetPlayer = true
				});
				list.Add((CardAction)new AAddCard
				{
					card = (Card)new ColorlessTrash(),
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
		result.cost = (((int)base.upgrade != 2) ? 1 : 2);
		result.infinite = (int)base.upgrade == 1;
		result.art = StableSpr.cards_Dodge;
		return result;
	}

	public override string Name()
	{
		return "Dirt Drive";
	}
}
