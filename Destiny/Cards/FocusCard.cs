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
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
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
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, exhaust = true };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 3 },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 1,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
				},
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = MagicFindManager.MagicFindStatus.Status, statusAmount = 2 } },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 2,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
				},
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = MagicFindManager.MagicFindStatus.Status, statusAmount = 2 } },
			],
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 2 },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 1,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
				},
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = MagicFindManager.MagicFindStatus.Status, statusAmount = 3 } },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 2,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
				},
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = MagicFindManager.MagicFindStatus.Status, statusAmount = 3 } },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 2 },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 1,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
				},
				new EnchantedAction { CardId = uuid, Level = 1, Action = new AStatus { targetPlayer = true, status = MagicFindManager.MagicFindStatus.Status, statusAmount = 2 } },
				new EnchantGateAction
				{
					CardId = uuid,
					Level = 2,
					Cost = ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shard), 1),
				},
				new EnchantedAction { CardId = uuid, Level = 2, Action = new AStatus { targetPlayer = true, status = MagicFindManager.MagicFindStatus.Status, statusAmount = 2 } },
			],
		};
}