using Microsoft.Xna.Framework;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DrainEssenceCard : Card, IDraculaCard
{
	public Matrix ModifyCardActionRenderMatrix(G g, List<CardAction> actions, CardAction action, int actionWidth)
	{
		if (upgrade == Upgrade.B)
			return Matrix.Identity;

		var spacing = 12 * g.mg.PIX_SCALE;
		var halfYCenterOffset = 16 * g.mg.PIX_SCALE;
		var index = actions.IndexOf(action);
		var recenterY = -(int)((index - actions.Count / 2.0 + 0.5) * spacing);
		return index switch
		{
			0 or 1 => Matrix.CreateTranslation(0, recenterY - halfYCenterOffset - spacing / 2 + spacing * index, 0),
			2 => Matrix.CreateTranslation(0, recenterY + halfYCenterOffset, 0),
			_ => Matrix.Identity
		};
	}

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("DrainEssence", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
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
			art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top,
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
