using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
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
				ModEntry.Instance.KokoroApi.ConditionalActions.Make(
					expression: ModEntry.Instance.KokoroApi.ConditionalActions.Equation(
						lhs: ModEntry.Instance.KokoroApi.ConditionalActions.Status(ModEntry.Instance.BleedingStatus.Status),
						@operator: IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual,
						rhs: ModEntry.Instance.KokoroApi.ConditionalActions.Constant(3),
						style: IKokoroApi.IConditionalActionApi.EquationStyle.Possession,
						hideOperator: true
					),
					action: ModEntry.Instance.KokoroApi.Actions.MakeStop(out var stopId)
				),
				..ModEntry.Instance.KokoroApi.Actions.MakeStopped(stopId, [
					new AStatus
					{
						targetPlayer = true,
						status = ModEntry.Instance.BleedingStatus.Status,
						statusAmount = 1
					},
					new ADrawCard { count = 1 }
				])
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
