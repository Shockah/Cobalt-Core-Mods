using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class PrototypeCard : Card, IRegisterable
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
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Prototype.png"), StableSpr.cards_QuickThinking).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototype", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 0 },
			a: () => new() { cost = 0, retain = true },
			b: () => new() { cost = 0 }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new ADrawCard { count = 1 },
			],
			a: () => [
				new ADrawCard { count = 1 },
			],
			b: () => [
				new ADrawCard { count = 1 },
				new OnAnalyzeAction { Action = new ADrawCard { count = 1 } },
			]
		);
}