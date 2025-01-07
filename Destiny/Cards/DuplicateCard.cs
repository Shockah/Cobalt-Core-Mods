using System;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class DuplicateCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Duplicate.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Duplicate", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, exhaust = true, retain = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.maxShard, statusAmount = 1 },
				new AVariableHint { status = Status.shard },
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = s.ship.Get(Status.shard), xHint = 1 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 1 },
				new AVariableHint { status = Status.shard },
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = Math.Min(s.ship.Get(Status.shard) + 1, s.ship.GetMaxShard()), xHint = 1 },
			],
			_ => [
				new AVariableHint { status = Status.shard },
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = s.ship.Get(Status.shard), xHint = 1 },
			],
		};
}