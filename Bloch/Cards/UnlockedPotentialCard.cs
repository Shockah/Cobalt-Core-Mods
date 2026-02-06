using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class UnlockedPotentialCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/UnlockedPotential.png"), StableSpr.cards_BigShield).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "UnlockedPotential", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.B => new() { cost = 3, exhaust = true },
			_ => new() { cost = 2, exhaust = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				ModEntry.Instance.Api.MakeChooseAura(card: this, amount: 3, actionId: 0),
				ModEntry.Instance.Api.MakeChooseAura(card: this, amount: 2, actionId: 1),
				new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = AuraManager.VeilingStatus.Status, statusAmount = 4 },
				new AStatus { targetPlayer = true, status = AuraManager.FeedbackStatus.Status, statusAmount = 3 },
				new AStatus { targetPlayer = true, status = AuraManager.InsightStatus.Status, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = AuraManager.VeilingStatus.Status, statusAmount = 3 },
				new AStatus { targetPlayer = true, status = AuraManager.FeedbackStatus.Status, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
			],
		};
}
