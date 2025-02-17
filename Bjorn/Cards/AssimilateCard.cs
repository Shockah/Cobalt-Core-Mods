using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class AssimilateCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Assimilate.png"), StableSpr.cards_Repairs).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Assimilate", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 1 },
			a: () => new() { cost = 1 },
			b: () => new() { cost = 1, exhaust = true }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new SmartShieldAction { Amount = 1 },
				new AnalyzeCostAction { CardId = uuid, RequireSelf = true, Action = new AHeal { targetPlayer = true, healAmount = 1, canRunAfterKill = true } },
			],
			a: () => [
				new SmartShieldAction { Amount = 2 },
				new AnalyzeCostAction { CardId = uuid, RequireSelf = true, Action = new AHeal { targetPlayer = true, healAmount = 1, canRunAfterKill = true } },
			],
			b: () => [
				new SmartShieldAction { Amount = 2 },
				new AnalyzeCostAction { CardId = uuid, RequireSelf = true, Count = 2, Action = new AHeal { targetPlayer = true, healAmount = 2, canRunAfterKill = true } },
			]
		);
}