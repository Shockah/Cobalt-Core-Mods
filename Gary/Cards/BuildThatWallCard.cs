using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Gary;

public sealed class BuildThatWallCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/BuildThatWall.png"), StableSpr.cards_goat).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BuildThatWall", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 2, exhaust = true },
			Upgrade.A => new() { cost = 3 },
			_ => new() { cost = 3, exhaust = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ASpawn { thing = new Asteroid(), offset = -1 }.SetCrammed(),
				new ASpawn { thing = new Asteroid() }.SetCrammed(),
				new ASpawn { thing = new Asteroid(), offset = 1 }.SetCrammed(),
			],
			_ => [
				new AStatus { targetPlayer = true, status = Cram.CramStatus.Status, statusAmount = 1 },
				new ASpawn { thing = new Asteroid(), offset = -1 },
				new ASpawn { thing = new Asteroid() },
				new ASpawn { thing = new Asteroid(), offset = 1 },
			],
		};
}