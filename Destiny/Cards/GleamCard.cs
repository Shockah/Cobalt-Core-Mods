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
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
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
	}

	public override CardData GetData(State state)
		=> new() { cost = 2 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AAttack { damage = GetDmg(s, 2) },
				new AStatus { targetPlayer = true, status = PristineShieldManager.PristineShieldStatus.Status, statusAmount = 1 },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 1,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 3),
				},
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = PristineShieldManager.PristineShieldStatus.Status, statusAmount = 1 } },
			],
			Upgrade.B => [
				new AAttack { damage = GetDmg(s, 3) },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 1,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
				},
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = PristineShieldManager.PristineShieldStatus.Status, statusAmount = 1 } },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 2,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
				},
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = PristineShieldManager.PristineShieldStatus.Status, statusAmount = 1 } },
			],
			_ => [
				new AAttack { damage = GetDmg(s, 2) },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 1,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
				},
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = PristineShieldManager.PristineShieldStatus.Status, statusAmount = 1 } },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 2,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 2),
				},
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = PristineShieldManager.PristineShieldStatus.Status, statusAmount = 1 } },
			],
		};
}