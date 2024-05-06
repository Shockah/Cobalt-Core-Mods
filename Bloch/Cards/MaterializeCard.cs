using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class MaterializeCard : Card, IRegisterable
{
	private static ISpriteEntry SatisfiedVeilingCostIcon = null!;
	private static ISpriteEntry UnsatisfiedVeilingCostIcon = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		SatisfiedVeilingCostIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/VeilingCostSatisfied.png"));
		UnsatisfiedVeilingCostIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/VeilingCostUnsatisfied.png"));

		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BlochDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
				dontOffer = true,
				unreleased = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/Materialize.png"), StableSpr.cards_Flux).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Materialize", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = 1,
			infinite = true,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							AuraManager.VeilingStatus.Status,
							UnsatisfiedVeilingCostIcon.Sprite,
							SatisfiedVeilingCostIcon.Sprite
						),
						amount: 1
					),
					action: ModEntry.Instance.KokoroApi.Actions.MakeContinue(out var continueId)
				),
				..ModEntry.Instance.KokoroApi.Actions.MakeContinued(continueId, [
					new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 3
					},
					new AStatus
					{
						targetPlayer = true,
						status = Status.shield,
						statusAmount = 1
					}
				])
			],
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							AuraManager.VeilingStatus.Status,
							UnsatisfiedVeilingCostIcon.Sprite,
							SatisfiedVeilingCostIcon.Sprite
						),
						amount: 1
					),
					action: new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 2
					}
				),
				new OnTurnEndManager.TriggerAction
				{
					Action = new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 2
					}
				}
			],
			_ => [
				ModEntry.Instance.KokoroApi.ActionCosts.Make(
					cost: ModEntry.Instance.KokoroApi.ActionCosts.Cost(
						resource: ModEntry.Instance.KokoroApi.ActionCosts.StatusResource(
							AuraManager.VeilingStatus.Status,
							UnsatisfiedVeilingCostIcon.Sprite,
							SatisfiedVeilingCostIcon.Sprite
						),
						amount: 1
					),
					action: new AStatus
					{
						targetPlayer = true,
						status = Status.tempShield,
						statusAmount = 3
					}
				)
			],
		};
}
