using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class SmartShieldDroneCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/SmartShieldDrone.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SmartShieldDrone", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 2 },
			a: () => new() { cost = 2 },
			b: () => new() { cost = 1 }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new ASpawn { thing = new SmartShieldDrone { targetPlayer = true } },
				new SmartShieldAction { Amount = 1 },
			],
			a: () => [
				new ASpawn { thing = new SmartShieldDrone { targetPlayer = true }, offset = -1 },
				new ASpawn { thing = new SmartShieldDrone { targetPlayer = true } },
			],
			b: () => [
				new ASpawn { thing = new SmartShieldDrone { targetPlayer = true } },
			]
		);
}