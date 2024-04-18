using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class IntrusiveThoughtCard : Card, IRegisterable
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
			Art = StableSpr.cards_TrashFumes,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/IntrusiveThought.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "IntrusiveThought", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			retain = true,
			unplayable = true
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.boost,
						statusAmount = 1
					}
				}
			],
			Upgrade.B => [
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.boost,
						statusAmount = 2
					}
				},
				new OnTurnEndManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.drawLessNextTurn,
						statusAmount = 2
					}
				}
			],
			_ => [
				new OnDiscardManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.boost,
						statusAmount = 1
					}
				},
				new OnTurnEndManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.drawLessNextTurn,
						statusAmount = 1
					}
				}
			]
		};
}
