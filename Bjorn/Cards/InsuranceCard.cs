using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class InsuranceCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Insurance.png"), StableSpr.cards_GoatDrone).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Insurance", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 2, flippable = true },
			a: () => new() { cost = 2, flippable = true, retain = true },
			b: () => new() { cost = 2, flippable = true, retain = true, exhaust = true }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new ASpawn { thing = MakeThing(true), offset = -1 },
				new ASpawn { thing = MakeThing(false), offset = 1 },
			],
			a: () => [
				new ASpawn { thing = MakeThing(true), offset = -1 },
				new ASpawn { thing = MakeThing(false), offset = 1 },
			],
			b: () => [
				new ASpawn { thing = MakeThing(!flipped), offset = flipped ? 1 : -1 },
				new ASpawn { thing = MakeThing(flipped) },
				new ASpawn { thing = MakeThing(!flipped), offset = flipped ? -1 : 1 },
			]
		);

	private static StuffBase MakeThing(bool attack)
		=> attack ? new AttackDrone() : new SmartShieldDrone { targetPlayer = true };
}