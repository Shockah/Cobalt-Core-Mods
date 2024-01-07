using Nickel;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class DrainEssenceCard : Card, IRegisterableCard
{
	public void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("DrainEssence", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DrainEssence", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			floppable = upgrade != Upgrade.B,
			recycle = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
					statusAmount = 2
				}.Disabled(flipped),
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							status: ModEntry.Instance.BleedingStatus.Status,
							target: IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow,
							costUnsatisfiedIcon: ModEntry.Instance.BleedingCostOff.Sprite,
							costSatisfiedIcon: ModEntry.Instance.BleedingCostOn.Sprite
						),
						amount: 1
					),
					action: new AStatus
					{
						targetPlayer = true,
						status = Status.shield,
						statusAmount = 1
					}
				).Disabled(flipped),
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							status: ModEntry.Instance.BleedingStatus.Status,
							target: IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow,
							costUnsatisfiedIcon: ModEntry.Instance.BleedingCostOff.Sprite,
							costSatisfiedIcon: ModEntry.Instance.BleedingCostOn.Sprite
						),
						amount: 2
					),
					action: new AHeal
					{
						targetPlayer = true,
						healAmount = 1
					}
				).Disabled(!flipped)
			],
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 1
				},
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							status: ModEntry.Instance.BleedingStatus.Status,
							target: IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow,
							costUnsatisfiedIcon: ModEntry.Instance.BleedingCostOff.Sprite,
							costSatisfiedIcon: ModEntry.Instance.BleedingCostOn.Sprite
						),
						amount: 1
					),
					action: new AStatus
					{
						targetPlayer = true,
						status = Status.shield,
						statusAmount = 1
					}
				),
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							status: ModEntry.Instance.BleedingStatus.Status,
							target: IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow,
							costUnsatisfiedIcon: ModEntry.Instance.BleedingCostOff.Sprite,
							costSatisfiedIcon: ModEntry.Instance.BleedingCostOn.Sprite
						),
						amount: 1
					),
					action: new AHeal
					{
						targetPlayer = true,
						healAmount = 1
					}
				)
			],
			_ => [
				new AStatus
				{
					targetPlayer = true,
					status = Status.shield,
					statusAmount = 1
				}.Disabled(flipped),
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							status: ModEntry.Instance.BleedingStatus.Status,
							target: IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow,
							costUnsatisfiedIcon: ModEntry.Instance.BleedingCostOff.Sprite,
							costSatisfiedIcon: ModEntry.Instance.BleedingCostOn.Sprite
						),
						amount: 1
					),
					action: new AStatus
					{
						targetPlayer = true,
						status = Status.shield,
						statusAmount = 1
					}
				).Disabled(flipped),
				new ADummyAction(),
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							status: ModEntry.Instance.BleedingStatus.Status,
							target: IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow,
							costUnsatisfiedIcon: ModEntry.Instance.BleedingCostOff.Sprite,
							costSatisfiedIcon: ModEntry.Instance.BleedingCostOn.Sprite
						),
						amount: 2
					),
					action: new AHeal
					{
						targetPlayer = true,
						healAmount = 1
					}
				).Disabled(!flipped)
			]
		};
}
