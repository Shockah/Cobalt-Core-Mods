using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class PowerWordCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/PowerWord.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "PowerWord", "name"]).Localize,
		});
		
		var shardResource = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard);
		Enchanted.SetEnchantLevelCost(entry.UniqueName, Upgrade.None, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
		Enchanted.SetEnchantLevelCost(entry.UniqueName, Upgrade.A, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
		Enchanted.SetEnchantLevelCost(entry.UniqueName, Upgrade.B, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
		Enchanted.SetEnchantLevelCost(entry.UniqueName, Upgrade.B, 2, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { Explosive.ExplosiveTrait };

	public override CardData GetData(State state)
		=> new() { cost = 2 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 3 },
				new EnchantGateAction { Level = 1 },
				new ImbueTraitAction { Level = 1, Trait = Explosive.ExplosiveTrait },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 2 },
				new EnchantGateAction { Level = 1 },
				new ImbueTraitAction { Level = 1, Trait = Explosive.ExplosiveTrait },
				new EnchantGateAction { Level = 2 },
				new ImbueTraitAction { Level = 2, Trait = Explosive.ExplosiveTrait },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 2 },
				new EnchantGateAction { Level = 1 },
				new ImbueTraitAction { Level = 1, Trait = Explosive.ExplosiveTrait },
			],
		};
}