using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class SafetyProtocolCard : Card, IRegisterable
{
	private int GetDataReentry;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/SafetyProtocol.png"), StableSpr.cards_Shield).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SafetyProtocol", "name"]).Localize,
		});

		helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, e) =>
		{
			if (e.Card is not SafetyProtocolCard card)
				return;

			card.GetDataReentry++;
			try
			{
				var unplayable = card.GetDataReentry == 1 && e.State.route is Combat { routeOverride: null } combat && (
					card.flipped
						? combat.energy < card.GetCurrentCost(MG.inst.g.state) + 1
						: combat.hand.All(someCard => !someCard.IsAnalyzable(MG.inst.g.state, combat))
				);
				
				if (unplayable)
					e.SetOverride(ModEntry.Instance.Helper.Content.Cards.UnplayableCardTrait, true);
			}
			finally
			{
				card.GetDataReentry--;
			}
		};
	}

	public override CardData GetData(State state)
		=> upgrade.Switch<CardData>(
			none: () => new() { cost = 0, floppable = true, infinite = true },
			a: () => new() { cost = 0, floppable = true, infinite = true },
			b: () => new() { cost = 0, floppable = true }
		);

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade.Switch<List<CardAction>>(
			none: () => [
				new AnalyzeCostAction { Action = new SmartShieldAction { Amount = 1 }, disabled = flipped },
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.EnergyResource, 1),
					new SmartShieldAction { Amount = 1 }
				).AsCardAction.Disabled(!flipped),
			],
			a: () => [
				new AnalyzeCostAction { Action = ModEntry.Instance.KokoroApi.ContinueStop.MakeTriggerAction(IKokoroApi.IV2.IContinueStopApi.ActionType.Continue, out var analyzeContinueId).AsCardAction, disabled = flipped },
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.EnergyResource, 1),
					ModEntry.Instance.KokoroApi.ContinueStop.MakeTriggerAction(IKokoroApi.IV2.IContinueStopApi.ActionType.Continue, out var energyContinueId).AsCardAction
				).AsCardAction.Disabled(!flipped),
				new ADummyAction(),
				.. ModEntry.Instance.KokoroApi.ContinueStop.MakeFlaggedActions(
					IKokoroApi.IV2.IContinueStopApi.ActionType.Continue,
					flipped ? energyContinueId : analyzeContinueId,
					[
						new SmartShieldAction { Amount = 1 },
						new ADrawCard { count = 1 }
					]
				).Select(a => a.AsCardAction),
			],
			b: () => [
				new AnalyzeCostAction { Action = new SmartShieldAction { Amount = 2 }, disabled = flipped },
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.EnergyResource, 1),
					new SmartShieldAction { Amount = 2 }
				).AsCardAction.Disabled(!flipped),
			]
		);
}