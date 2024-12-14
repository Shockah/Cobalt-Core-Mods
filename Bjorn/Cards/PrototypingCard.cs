using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class PrototypingCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Prototyping.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Prototyping", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "Prototyping", "description", upgrade.ToString()]);
		return upgrade.Switch<CardData>(
			none: () => new() { cost = 2, exhaust = true, description = description },
			a: () => new() { cost = 1, exhaust = true, description = description },
			b: () => new() { cost = 3, exhaust = true, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AAddCard { card = new PrototypeCard(), destination = CardDestination.Hand }
			],
			a: () => [
				new AAddCard { card = new PrototypeCard(), destination = CardDestination.Hand }
			],
			b: () => [
				new AAddCard { card = new PrototypeCard(), destination = CardDestination.Hand, amount = 2 }
			]
		);
}