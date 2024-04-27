using System.Collections.Generic;
using EvilRiggs.CardActions;

namespace EvilRiggs.Cards.Uncommon;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.uncommon)]
internal class TargetLockV2 : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)(object)new ATargetLock());
				break;
			case 1:
				list.Add((CardAction)(object)new ATargetLock());
				break;
			case 2:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)3
					}
				});
				list.Add((CardAction)(object)new ATargetLock());
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 0;
		result.retain = (int)base.upgrade == 1;
		result.art = StableSpr.cards_MultiBlast;
		return result;
	}

	public override string Name()
	{
		return "Target Lock";
	}
}
