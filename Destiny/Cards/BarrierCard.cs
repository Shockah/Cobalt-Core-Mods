using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class BarrierCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Barrier.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Barrier", "name"]).Localize,
		});
		
		var shardResource = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard);
		Enchanted.SetEnchantLevelCost(entry.UniqueName, Upgrade.None, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
		Enchanted.SetEnchantLevelCost(entry.UniqueName, Upgrade.B, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
		Enchanted.SetEnchantLevelCost(entry.UniqueName, Upgrade.B, 2, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 3, retain = true },
			Upgrade.B => new() { cost = 3 },
			_ => new() { cost = 3 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 6 },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 6 },
				new EnchantGateAction { Level = 1 },
				new ImbueAction { Level = 1, Trait = ModEntry.Instance.Helper.Content.Cards.RetainCardTrait },
				new EnchantGateAction { Level = 2 },
				new ImbueAction { Level = 2, Trait = ModEntry.Instance.Helper.Content.Cards.RecycleCardTrait },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 6 },
				new EnchantGateAction { Level = 1 },
				new ImbueAction { Level = 1, Trait = ModEntry.Instance.Helper.Content.Cards.RetainCardTrait },
			],
		};
}