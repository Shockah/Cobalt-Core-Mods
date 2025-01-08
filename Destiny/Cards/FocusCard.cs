using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class FocusCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Focus.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Focus", "name"]).Localize,
		});
		
		var shardResource = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard);
		Enchanted.SetEnchantLevelCost(entry.UniqueName, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 1));
		Enchanted.SetEnchantLevelCost(entry.UniqueName, 2, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 1));
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, exhaust = true, art = Enchanted.GetCardArt(this), artTint = "ffffff" };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 3 },
				new EnchantGateAction { Level = 1 },
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 2 } },
				new EnchantGateAction { Level = 2 },
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 2 } },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 2 },
				new EnchantGateAction { Level = 1 },
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 3 } },
				new EnchantGateAction { Level = 2 },
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 3 } },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 2 },
				new EnchantGateAction { Level = 1 },
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 2 } },
				new EnchantGateAction { Level = 2 },
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = MagicFind.MagicFindStatus.Status, statusAmount = 2 } },
			],
		};
}