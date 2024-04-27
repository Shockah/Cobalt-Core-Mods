using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.rare, unreleased = true)]
internal class RocketSalvo : Card
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
					}
				});
				list.Add((CardAction)new AAddCard
				{
					card = (Card)new TrashFumes(),
					destination = (CardDestination)0,
					amount = 2
				});
				break;
			case 1:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					}
				});
				list.Add((CardAction)new AAddCard
				{
					card = (Card)new TrashFumes(),
					destination = (CardDestination)0,
					amount = 2
				});
				break;
			case 2:
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
				list.Add((CardAction)new AAddCard
				{
					card = (Card)new TrashFumes(),
					destination = (CardDestination)0,
					amount = 3
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 0;
		result.infinite = true;
		result.art = StableSpr.cards_SeekerMissileCard;
		return result;
	}

	public override string Name()
	{
		return "Rocket Salvo";
	}
}
