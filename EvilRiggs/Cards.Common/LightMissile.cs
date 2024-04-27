using System.Collections.Generic;
using EvilRiggs.Drones;

namespace EvilRiggs.Cards.Common;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.common)]
internal class LightMissile : Card
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
					thing = (StuffBase)(object)new MissileLight
					{
						yAnimation = 0.0
					}
				});
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = (Status)11,
					statusAmount = 1
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
				list.Add((CardAction)new AStatus
				{
					targetPlayer = true,
					status = (Status)11,
					statusAmount = 1
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
		result.cost = (((int)base.upgrade != 2) ? 1 : 0);
		result.exhaust = (int)base.upgrade == 2;
		result.art = StableSpr.cards_SeekerMissileCard;
		return result;
	}

	public override string Name()
	{
		return "Light Missile";
	}
}
