using Nickel;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class BloodScentCard : Card, IDraculaCard
{
	public void Register(IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("BloodScent", new()
		{
			CardType = GetType(),
			Meta = new()
			{
				deck = ModEntry.Instance.DraculaDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "BloodScent", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			floppable = upgrade == Upgrade.A
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 2
				}.Disabled(flipped),
				new ADummyAction(),
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
					statusAmount = 1
				}.Disabled(!flipped),
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							status: ModEntry.Instance.BleedingStatus.Status,
							target: IKokoroApi.IActionCostApi.StatusResourceTarget.EnemyWithOutgoingArrow,
							costUnsatisfiedIcon: ModEntry.Instance.BleedingCostOff.Sprite,
							costSatisfiedIcon: ModEntry.Instance.BleedingCostOn.Sprite
						),
						amount: 4
					),
					action: new AStatus
					{
						targetPlayer = true,
						status = Status.overdrive,
						statusAmount = 2
					}
				).Disabled(!flipped)
			],
			Upgrade.B => [
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
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
						amount: 4
					),
					action: new AStatus
					{
						targetPlayer = true,
						status = Status.powerdrive,
						statusAmount = 1
					}
				)
			],
			_ => [
				new AStatus
				{
					targetPlayer = false,
					status = ModEntry.Instance.BleedingStatus.Status,
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
						amount: 4
					),
					action: new AStatus
					{
						targetPlayer = true,
						status = Status.overdrive,
						statusAmount = 2
					}
				)
			]
		};
}
