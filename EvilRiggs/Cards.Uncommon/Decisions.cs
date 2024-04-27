using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.uncommon, unreleased = true)]
internal class Decisions : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new AAttack
				{
					damage = 1,
					disabled = base.flipped
				});
				list.Add((CardAction)new ADrawCard
				{
					count = 1,
					disabled = base.flipped
				});
				list.Add((CardAction)new ADummyAction());
				list.Add((CardAction)new AAttack
				{
					damage = 3,
					disabled = !base.flipped
				});
				list.Add((CardAction)new ADiscard
				{
					count = 1,
					disabled = !base.flipped
				});
				break;
			case 1:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					}
				});
				list.Add((CardAction)new ADrawCard
				{
					count = 2
				});
				break;
			case 2:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					}
				});
				list.Add((CardAction)new ADiscard
				{
					count = 1
				});
				list.Add((CardAction)new ADrawCard
				{
					count = 2
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 1;
		result.floppable = true;
		result.art = (Spr)(base.flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top);
		return result;
	}

	public override string Name()
	{
		return "Decisions";
	}
}
