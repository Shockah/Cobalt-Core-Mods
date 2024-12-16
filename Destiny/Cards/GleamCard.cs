using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class GleamCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DestinyDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Gleam.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Gleam", "name"]).Localize,
		});
		
		EnchantedManager.SetEnchantCosts(entry.UniqueName, Upgrade.None, [
			ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
			ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 2),
		]);
		EnchantedManager.SetEnchantCosts(entry.UniqueName, Upgrade.None, [
			ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 3),
		]);
		EnchantedManager.SetEnchantCosts(entry.UniqueName, Upgrade.None, [
			ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
			ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
		]);
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 2, exhaust = true, buoyant = true },
			Upgrade.B => new() { cost = 2, exhaust = true },
			_ => new() { cost = 2, exhaust = true, buoyant = true },
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