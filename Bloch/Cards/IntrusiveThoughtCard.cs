using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/IntrusiveThought.png"), StableSpr.cards_TrashFumes).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "IntrusiveThought", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 0 : 1,
			retain = true,
			recycle = upgrade == Upgrade.B,
			unplayable = upgrade != Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new OnDiscardManager.TriggerAction
				{
					Action = new ADrawCard { count = 3 }
				}
			],
			_ => [
				new OnDiscardManager.TriggerAction
				{
					Action = new ADrawCard { count = 3 }
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
