using System.Collections.Generic;

namespace EvilRiggs.Cards.Rare;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.rare)]
internal class Strangelove : Card
{
	public override List<CardAction> GetActions(State s, Combat c)
	{
		List<CardAction> list = new List<CardAction>();
		Status status = StatusMeta.deckToMissingStatus[(Deck)Manifest.EvilRiggsDeck.Id!.Value];
		Upgrade upgrade = base.upgrade;
		Upgrade val = upgrade;
		switch ((int)val)
		{
			case 0:
				list.Add((CardAction)new AStatus
				{
					status = status,
					statusAmount = 99,
					targetPlayer = true
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = -1
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = 0
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = 1
				});
				break;
			case 1:
				list.Add((CardAction)new AStatus
				{
					status = status,
					statusAmount = 5,
					targetPlayer = true
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = -1
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = 0
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = 1
				});
				break;
			case 2:
				list.Add((CardAction)new AStatus
				{
					status = status,
					statusAmount = 99,
					targetPlayer = true
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = -2
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = -1
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = 1
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					offset = 2
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 3;
		result.exhaust = true;
		result.art = StableSpr.cards_SeekerMissileCard;
		return result;
	}

	public override string Name()
	{
		return "Strangelove";
	}
}
