using System.Collections.Generic;
using EvilRiggs.CardActions;

namespace EvilRiggs.Cards.Common;

[CardMeta(upgradesTo = [Upgrade.A, Upgrade.B], rarity = Rarity.common)]
internal class JammedBarrel : Card
{
	public override void OnDraw(State s, Combat c)
	{
		base.flipped = false;
	}

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
					thing = (StuffBase)new Asteroid
					{
						yAnimation = 0.0
					},
					disabled = base.flipped
				});
				list.Add((CardAction)(object)new ASequential
				{
					targetCard = (Card?)(object)this
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					},
					disabled = !base.flipped
				});
				break;
			case 1:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Asteroid
					{
						yAnimation = 0.0,
						bubbleShield = true
					},
					disabled = base.flipped
				});
				list.Add((CardAction)(object)new ASequential
				{
					targetCard = (Card?)(object)this
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)0
					},
					disabled = !base.flipped
				});
				break;
			case 2:
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Asteroid
					{
						yAnimation = 0.0
					},
					disabled = base.flipped
				});
				list.Add((CardAction)(object)new ASequential
				{
					targetCard = (Card?)(object)this
				});
				list.Add((CardAction)new ASpawn
				{
					thing = (StuffBase)new Missile
					{
						yAnimation = 0.0,
						missileType = (MissileType)1
					},
					disabled = !base.flipped
				});
				break;
		}
		return list;
	}

	public override CardData GetData(State state)
	{
		CardData result = new();
		result.cost = 1;
		result.floppable = false;
		result.infinite = true;
		result.art = (Spr)(base.flipped ? Manifest.sprites["cardart_seq_normal_bottom"].Id!.Value : Manifest.sprites["cardart_seq_normal_top"].Id!.Value);
		result.artTint = "FFFFFF";
		return result;
	}

	public override string Name()
	{
		return "Jammed Barrel";
	}
}
