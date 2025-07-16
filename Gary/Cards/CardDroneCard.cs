using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class CardDroneCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.GaryDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/CardDrone.png"), StableSpr.cards_goat).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "CardDrone", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 2, exhaust = true },
			_ => new() { cost = 2 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ASpawn { thing = new CardDroneManager.Drone() },
				new ASpawn { thing = new CardDroneManager.Drone() },
			],
			Upgrade.A => [
				new ASpawn { thing = new CardDroneManager.Drone() },
				new AStatus { targetPlayer = true, status = Status.droneShift, statusAmount = 1 },
			],
			_ => [
				new ASpawn { thing = new CardDroneManager.Drone() },
			],
		};
}