using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class GoForBrokeCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DestinyDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/GoForBroke.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "GoForBroke", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, retain = true },
			Upgrade.B => new() { cost = 0, exhaust = true, retain = true },
			_ => new() { cost = 2, retain = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1, shardcost = 2 },
				new AStatus { targetPlayer = true, status = Status.maxShard, statusAmount = -1 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.maxShard, statusAmount = -1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1, shardcost = 2 },
				new AStatus { targetPlayer = true, status = Status.maxShard, statusAmount = -1 },
			],
		};
}