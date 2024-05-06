using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class ThoughtFilterCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/ThoughtFilter.png"), StableSpr.cards_ColorlessTrash).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ThoughtFilter", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			retain = true,
			recycle = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new OnTurnEndManager.TriggerAction
				{
					Action = new ScryAction { Amount = 3 }
				},
				new OnTurnEndManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.shield,
						statusAmount = -1
					}
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
					statusAmount = 2
				}
			],
			Upgrade.A => [
				new OnTurnEndManager.TriggerAction
				{
					Action = new ScryAction { Amount = 2 }
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
					statusAmount = 2
				}
			],
			_ => [
				new OnTurnEndManager.TriggerAction
				{
					Action = new ScryAction { Amount = 2 }
				},
				new OnTurnEndManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.shield,
						statusAmount = -1
					}
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
					statusAmount = 2
				}
			]
		};
}
