using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class FractalStructureCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/FractalStructure.png"), StableSpr.cards_BoostCapacitors).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "FractalStructure", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 1 },
			a: () => new() { cost = 1 },
			b: () => new() { cost = 0, exhaust = true }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AnalyzeCostAction { Action = new AStatus { targetPlayer = true, status = Status.maxShield, statusAmount = 1 } },
				new SmartShieldAction { Amount = 1 },
			],
			a: () => [
				new AnalyzeCostAction { Action = new AStatus { targetPlayer = true, status = Status.maxShield, statusAmount = 1 } },
				new SmartShieldAction { Amount = 2 },
			],
			b: () => [
				new AnalyzeCostAction { Count = 2, Action = new AStatus { targetPlayer = true, status = Status.maxShield, statusAmount = 2 } },
				new SmartShieldAction { Amount = 2 },
			]
		);
}