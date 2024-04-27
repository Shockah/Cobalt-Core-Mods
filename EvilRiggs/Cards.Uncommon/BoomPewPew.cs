using System;
using System.Collections.Generic;

namespace EvilRiggs.Cards.Uncommon;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.uncommon, unreleased = true)]
internal class BoomPewPew : Card
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
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					}
				});
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -1,
					isRandom = true
				});
				list.Add((CardAction)new AAttack
				{
					damage = 1
				});
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -1,
					isRandom = true
				});
				list.Add((CardAction)new AAttack
				{
					damage = 1
				});
				break;
			case 1:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0,
						bubbleShield = true
					}
				});
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -1,
					isRandom = true
				});
				list.Add((CardAction)new AAttack
				{
					damage = 1
				});
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -1,
					isRandom = true
				});
				list.Add((CardAction)new AAttack
				{
					damage = 1
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
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -2,
					isRandom = true
				});
				list.Add((CardAction)new AAttack
				{
					damage = 1
				});
				list.Add((CardAction)new AMove
				{
					targetPlayer = true,
					dir = -2,
					isRandom = true
				});
				list.Add((CardAction)new AAttack
				{
					damage = 1
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 1;
		result.art = StableSpr.cards_riggs;
		return result;
	}

	public override string Name()
	{
		return "Boom Pew Pew";
	}
}
