using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class HoneCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Hone.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Hone", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 1, exhaust = true, buoyant = true },
			Upgrade.B => new() { cost = 1, exhaust = true },
			_ => new() { cost = 1, exhaust = true, buoyant = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.maxShard, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = MagicFindManager.MagicFindStatus.Status, statusAmount = 2 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.maxShard, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 3 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.maxShard, statusAmount = 1 },
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 1 },
			],
		};
}