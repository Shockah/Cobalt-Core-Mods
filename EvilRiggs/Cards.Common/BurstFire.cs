using System.Collections.Generic;

namespace EvilRiggs.Cards.Common;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.common)]
internal class BurstFire : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					},
					offset = -1
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					},
					offset = 1
				});
				break;
			case 1:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					},
					offset = -2
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					},
					offset = 0
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					},
					offset = 2
				});
				break;
			case 2:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					}
				});
				list.Add((CardAction)new ADroneMove
				{
					dir = 1,
					isRandom = true
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = (((int)base.upgrade == 2) ? 1 : 2);
		result.infinite = (int)base.upgrade == 2;
		result.art = StableSpr.cards_SeekerMissileCard;
		return result;
	}

	public override string Name()
	{
		return "Burst Fire";
	}
}
