using Nanoray.PluginManager;
using Nickel;
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
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Flux,
			//Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Focus.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Focus", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.A ? 0 : 1,
			infinite = upgrade == Upgrade.B
		};

	public override List<CardAction> GetActions(State s, Combat c)
	{
		if (upgrade == Upgrade.B)
		{
			return [
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
			];
		}
		else
		{
			var veiling = s.ship.Get(AuraManager.VeilingStatus.Status);
			return [
				new AVariableHint
				{
					status = AuraManager.VeilingStatus.Status,
				},
				new AStatus
				{
					targetPlayer = true,
					status = Status.tempShield,
					statusAmount = veiling * 2,
					xHint = 2,
				},
				new AStatus
				{
					targetPlayer = true,
					mode = AStatusMode.Set,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 0,
				}
			];
		}
	}
}
