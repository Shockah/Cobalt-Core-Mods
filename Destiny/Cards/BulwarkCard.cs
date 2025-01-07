using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Destiny;

public sealed class BulwarkCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Bulwark.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Bulwark", "name"]).Localize,
		});

		var shardResource = ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard);
		Enchanted.SetEnchantLevelCost(entry.UniqueName, Upgrade.A, 1, ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(shardResource, 2));
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 2 },
			Upgrade.B => new() { cost = 2, exhaust = true },
			_ => new() { cost = 2 },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = PristineShield.PristineShieldStatus.Status, statusAmount = 2 },
				new EnchantGateAction { Level = 1 },
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = PristineShield.PristineShieldStatus.Status, statusAmount = 1 } },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.perfectShield, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = PristineShield.PristineShieldStatus.Status, statusAmount = 2 },
			],
		};
}