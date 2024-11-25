using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class AuraOfDarknessCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("AuraOfDarkness", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Corrode,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "AuraOfDarkness", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 0,
			recycle = upgrade != Upgrade.B,
			infinite = upgrade == Upgrade.B,
			unplayable = upgrade == Upgrade.B && state.ship.Get(ModEntry.Instance.BleedingStatus.Status) >= 3,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.Equation(
						ModEntry.Instance.KokoroApi.Conditional.Status(ModEntry.Instance.BleedingStatus.Status),
						IKokoroApi.IV2.IConditionalApi.EquationOperator.GreaterThanOrEqual,
						ModEntry.Instance.KokoroApi.Conditional.Constant(3),
						IKokoroApi.IV2.IConditionalApi.EquationStyle.Possession
					).SetShowOperator(false),
					ModEntry.Instance.KokoroApi.ContinueStop.MakeTriggerAction(IKokoroApi.IV2.IContinueStopApi.ActionType.Stop, out var stopId).AsCardAction
				).AsCardAction,
				.. ModEntry.Instance.KokoroApi.ContinueStop.MakeFlaggedActions(IKokoroApi.IV2.IContinueStopApi.ActionType.Stop, stopId, [
					new AStatus
					{
						targetPlayer = true,
						status = ModEntry.Instance.BleedingStatus.Status,
						statusAmount = 1
					},
					new ADrawCard { count = 1 }
				]).Select(a => a.AsCardAction)
			],
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.drawNextTurn,
					statusAmount = 1
				}
			],
			_ => [
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				},
				new AStatus
				{
					targetPlayer = true,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				}
			]
		};
}
