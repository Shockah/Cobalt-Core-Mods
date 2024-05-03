using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class PainReactionCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/PainReaction.png"), StableSpr.cards_Panic).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PainReaction", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			retain = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new ADrawCard
				{
					count = 1
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 3
					}
				}
			],
			Upgrade.B => [
				new ADrawCard
				{
					count = 1
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 2
					}
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new AHeal
					{
						targetPlayer = true,
						healAmount = 2,
						canRunAfterKill = true,
					}
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new ExhaustCardAction
					{
						CardId = uuid
					}
				}
			],
			_ => [
				new ADrawCard
				{
					count = 1
				},
				new OnHullDamageManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 2
					}
				}
			]
		};
}
