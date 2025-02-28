using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.CatExpansion;

public sealed class SwordAndShieldCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/SwordAndShield.png"), StableSpr.cards_ShieldGun).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SwordAndShield", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 2 },
			Upgrade.B => new() { cost = 3, exhaust = true },
			_ => new() { cost = 2 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new ASpawn { thing = new Missile { yAnimation = 0, targetPlayer = false, missileType = MissileType.heavy }, offset = -1 },
				new ASpawn { thing = new Geode { bubbleShield = true } },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 2 },
			],
			Upgrade.B => [
				new ASpawn { thing = new Missile { yAnimation = 0, targetPlayer = false, missileType = MissileType.heavy }, offset = -1 },
				new ASpawn { thing = new Geode { bubbleShield = true } },
				new ASpawn { thing = new Missile { yAnimation = 0, targetPlayer = false, missileType = MissileType.heavy }, offset = 1 },
				new ASpawn { thing = new Geode { bubbleShield = true }, offset = 2 },
			],
			_ => [
				new ASpawn { thing = new Missile { yAnimation = 0, targetPlayer = false, missileType = MissileType.heavy }, offset = -1 },
				new ASpawn { thing = new Geode { bubbleShield = true } },
			],
		};
}