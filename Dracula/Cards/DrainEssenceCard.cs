using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class DrainEssenceCard : Card, IDraculaCard
{
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
			Art = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/DrainEssence.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DrainEssence", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new()
		{
			cost = upgrade == Upgrade.B ? 0 : 1,
			exhaust = upgrade == Upgrade.B,
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					new AHeal { targetPlayer = true, healAmount = 1 }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 }
				).AsCardAction,
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 },
			],
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.VariableHintTargetPlayer.MakeVariableHint(
					new AVariableHint { status = ModEntry.Instance.BleedingStatus.Status }
				).SetTargetPlayer(false).AsCardAction,
				new AHeal { targetPlayer = true, healAmount = c.otherShip.Get(ModEntry.Instance.BleedingStatus.Status), xHint = 1 },
				new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, mode = AStatusMode.Set, statusAmount = 0 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 },
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 }
				).AsCardAction,
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(
						ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1
					),
					new AHeal { targetPlayer = true, healAmount = 1 }
				).AsCardAction,
			]
		};
}
