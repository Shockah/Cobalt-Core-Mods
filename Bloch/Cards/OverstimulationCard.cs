using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class OverstimulationCard : Card, IRegisterable
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
			Art = StableSpr.cards_Vamoose,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Overstimulation.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Overstimulation", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 1 : 2,
			exhaust = true,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = ModEntry.Instance.KokoroApi.OxidationVanillaStatus,
						statusAmount = 4
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.InsightStatus.Status,
						statusAmount = 4
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.FeedbackStatus.Status,
						statusAmount = 4
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.VeilingStatus.Status,
						statusAmount = 4
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.IntensifyStatus.Status,
						statusAmount = 1
					}
				},
			],
			Upgrade.A => [
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = ModEntry.Instance.KokoroApi.OxidationVanillaStatus,
						statusAmount = 3
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.InsightStatus.Status,
						statusAmount = 2
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.FeedbackStatus.Status,
						statusAmount = 2
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.VeilingStatus.Status,
						statusAmount = 3
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.IntensifyStatus.Status,
						statusAmount = 1
					}
				},
			],
			_ => [
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = ModEntry.Instance.KokoroApi.OxidationVanillaStatus,
						statusAmount = 3
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.InsightStatus.Status,
						statusAmount = 2
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.FeedbackStatus.Status,
						statusAmount = 2
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.VeilingStatus.Status,
						statusAmount = 2
					}
				},
				new OncePerTurnManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = AuraManager.IntensifyStatus.Status,
						statusAmount = 1
					}
				},
			]
		};
}
