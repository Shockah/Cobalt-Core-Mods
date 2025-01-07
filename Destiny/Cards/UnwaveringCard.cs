using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class UnwaveringCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Unwavering.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Unwavering", "name"]).Localize,
		});
		
		var shardResource = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard);
		EnchantedManager.SetEnchantLevelCost(entry.UniqueName, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 1));
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0 },
			Upgrade.B => new() { cost = 1, retain = true },
			_ => new() { cost = 1 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> [
			new AStatus { targetPlayer = true, status = PristineShieldManager.PristineShieldStatus.Status, statusAmount = 1 },
			new EnchantGateAction { Level = 1 },
			new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 } },
			new ImbueAction { Level = 1, Trait = ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait },
		];
}