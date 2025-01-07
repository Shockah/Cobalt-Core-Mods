using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class ImmovableObjectCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/ImmovableObject.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ImmovableObject", "name"]).Localize,
		});
		
		var shardResource = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard);
		EnchantedManager.SetEnchantLevelCost(entry.UniqueName, Upgrade.None, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 3));
		EnchantedManager.SetEnchantLevelCost(entry.UniqueName, Upgrade.None, 2, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 3));
		EnchantedManager.SetEnchantLevelCost(entry.UniqueName, Upgrade.A, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 3));
		EnchantedManager.SetEnchantLevelCost(entry.UniqueName, Upgrade.A, 2, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 3));
		EnchantedManager.SetEnchantLevelCost(entry.UniqueName, Upgrade.B, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
		EnchantedManager.SetEnchantLevelCost(entry.UniqueName, Upgrade.B, 2, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 3, exhaust = true, retain = true },
			Upgrade.B => new() { cost = 3, exhaust = true },
			_ => new() { cost = 3, exhaust = true },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 },
				new EnchantGateAction { Level = 1 },
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 } },
				new EnchantGateAction { Level = 2 },
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 } },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 },
				new EnchantGateAction { Level = 1 },
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 } },
				new EnchantGateAction { Level = 2 },
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 } },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 },
				new EnchantGateAction { Level = 1 },
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 } },
				new EnchantGateAction { Level = 2 },
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 } },
			],
		};
}