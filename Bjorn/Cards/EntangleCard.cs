using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

public sealed class EntangleCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Entangle.png"), StableSpr.cards_Dodge).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Entangle", "name"]).Localize,
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
				new AStatus { targetPlayer = true, status = EntanglementManager.EntanglementStatus.Status, statusAmount = 1 },
			],
			a: () => [
				new AStatus { targetPlayer = true, status = EntanglementManager.EntanglementStatus.Status, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = 1 },
			],
			b: () => [
				new AStatus { targetPlayer = true, status = EntanglementManager.EntanglementStatus.Status, statusAmount = 3 },
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = 2 },
			]
		);
}