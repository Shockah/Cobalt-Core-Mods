using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class CrashCard : Card, IRegisterable, IHasCustomCardTraits
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Crash.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Crash", "name"]).Localize,
		});
		
		var shardResource = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard);
		EnchantedManager.SetEnchantLevelCost(entry.UniqueName, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ExplosiveManager.ExplosiveTrait };

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0 },
			Upgrade.B => new() { cost = 1 },
			_ => new() { cost = 1 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 0) },
				new EnchantGateAction { Level = 1 },
				new ImbueAction { Level = 1, Trait = ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait },
			],
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 1) },
				new EnchantGateAction { Level = 1 },
				new ImbueAction { Level = 1, Trait = ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 0) },
				new EnchantGateAction { Level = 1 },
				new ImbueAction { Level = 1, Trait = ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait },
			],
		};
}