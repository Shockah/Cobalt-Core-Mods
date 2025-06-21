using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.Dracula;

internal sealed class BloodScentCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("BloodScent", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/BloodScent.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BloodScent", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			exhaust = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					ModEntry.Instance.KokoroApi.ContinueStop.MakeTriggerAction(IKokoroApi.IV2.IContinueStopApi.ActionType.Continue, out var continueId).AsCardAction
				).AsCardAction,
				.. ModEntry.Instance.KokoroApi.ContinueStop.MakeFlaggedActions(IKokoroApi.IV2.IContinueStopApi.ActionType.Continue, continueId, [
					new AHurt { targetPlayer = false, hurtAmount = 2 },
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
				]).Select(a => a.AsCardAction),
			],
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 2
					),
					ModEntry.Instance.KokoroApi.ContinueStop.MakeTriggerAction(IKokoroApi.IV2.IContinueStopApi.ActionType.Continue, out var continueId).AsCardAction
				).AsCardAction,
				.. ModEntry.Instance.KokoroApi.ContinueStop.MakeFlaggedActions(IKokoroApi.IV2.IContinueStopApi.ActionType.Continue, continueId, [
					new AHurt { targetPlayer = false, hurtAmount = 3 },
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 3 },
				]).Select(a => a.AsCardAction),
			],
			_ => [
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					ModEntry.Instance.KokoroApi.ContinueStop.MakeTriggerAction(IKokoroApi.IV2.IContinueStopApi.ActionType.Continue, out var continueId).AsCardAction
				).AsCardAction,
				.. ModEntry.Instance.KokoroApi.ContinueStop.MakeFlaggedActions(IKokoroApi.IV2.IContinueStopApi.ActionType.Continue, continueId, [
					new AHurt { targetPlayer = false, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
				]).Select(a => a.AsCardAction),
			]
		};
}
