using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class TearCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Tear", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.common,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_HandCannon,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Tear", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AHurt { targetPlayer = false, hurtShieldsFirst = true, hurtAmount = 1 },
				new AAttack { damage = GetDmg(s, 0), status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 2 },
			],
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1),
					new AHurt { targetPlayer = false, hurtAmount = 1 }
				).AsCardAction,
				new AAttack { damage = GetDmg(s, 0), status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 2 },
			],
			_ => [
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1),
					new AHurt { targetPlayer = false, hurtShieldsFirst = true, hurtAmount = 1 }
				).AsCardAction,
				new AAttack { damage = GetDmg(s, 0), status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 2 },
			]
		};
}