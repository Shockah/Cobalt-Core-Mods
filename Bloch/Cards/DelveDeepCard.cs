using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class DelveDeepCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/DelveDeep.png"), StableSpr.cards_Prepare).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DelveDeep", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			exhaust = upgrade != Upgrade.A,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new ScryAction { Amount = 6 },
				new AStatus
				{
					targetPlayer = true,
					status = Status.drawNextTurn,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyNextTurn,
					statusAmount = 1
				}
			],
			_ => [
				new ScryAction { Amount = 5 },
				new AStatus
				{
					targetPlayer = true,
					status = Status.energyNextTurn,
					statusAmount = 1
				}
			]
		};
}
