using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class AttentionSpanCard : Card, IRegisterable
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
			Art = StableSpr.cards_Ace,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/AttentionSpan.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "AttentionSpan", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			recycle = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
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
						status = AuraManager.FeedbackStatus.Status,
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
				new OnTurnEndManager.TriggerAction
				{
					Action = new ExhaustCardAction
					{
						CardId = uuid
					}
				}
			],
			_ => [
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
				new OnTurnEndManager.TriggerAction
				{
					Action = new ExhaustCardAction
					{
						CardId = uuid
					}
				}
			]
		};
}
