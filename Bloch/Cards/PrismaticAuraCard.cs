using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class PrismaticAuraCard : Card, IRegisterable
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
			Art = StableSpr.cards_SecondOpinions,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/PrismaticAura.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PrismaticAura", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			artTint = "ffffff",
			cost = upgrade == Upgrade.B ? 1 : 0,
			description = upgrade == Upgrade.B ? ModEntry.Instance.Localizations.Localize(["card", "PrismaticAura", "description", upgrade.ToString()]) : null,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.InsightStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.FeedbackStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 1
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.InsightStatus.Status,
						statusAmount = 1
					}
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.FeedbackStatus.Status,
						statusAmount = 1
					}
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.VeilingStatus.Status,
						statusAmount = 1
					}
				},
			],
			_ => [
				new ADummyAction(),
				new OnTurnEndManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.InsightStatus.Status,
						statusAmount = upgrade == Upgrade.A ? 2 : 1
					}
				},
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.FeedbackStatus.Status,
						statusAmount = upgrade == Upgrade.A ? 2 : 1
					}
				},
				new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = upgrade == Upgrade.A ? 2 : 1
				},
			]
		};
}
