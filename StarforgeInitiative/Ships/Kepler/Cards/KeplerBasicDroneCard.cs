using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerBasicDroneCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = Deck.colorless,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Kepler/Card/BasicDrone.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Kepler", "card", "BasicDrone", "name"]).Localize,
		});
	}
	
	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1 },
			Upgrade.B => new() { cost = 1, exhaust = true },
			_ => new() { cost = 1, exhaust = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
			],
			Upgrade.B => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false, upgraded = true } },
			],
			_ => [
				new ASpawn { thing = new AttackDrone { targetPlayer = false } },
			],
		};
}