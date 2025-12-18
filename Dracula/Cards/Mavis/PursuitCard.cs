using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dracula;

internal sealed class PursuitCard : Card, IDraculaCard
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard("Pursuit", new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.MavisDeck.Deck,
				rarity = Rarity.uncommon,
				upgradesTo = [Upgrade.A, Upgrade.B]
			},
			Art = StableSpr.cards_Corrode,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Mavis", "Pursuit", "name"]).Localize
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1 };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.loseEvadeNextTurn, statusAmount = 1 },
				new AStatus { targetPlayer = false, status = ModEntry.Instance.BleedingStatus.Status, statusAmount = 1 },
			],
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.ActionCosts.MakeCostAction(
					ModEntry.Instance.KokoroApi.ActionCosts.MakeResourceCost(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(ModEntry.Instance.BleedingStatus.Status, false), 1),
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 3 }
				).AsCardAction,
				new AStatus { targetPlayer = true, status = Status.loseEvadeNextTurn, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
				new AStatus { targetPlayer = true, status = Status.loseEvadeNextTurn, statusAmount = 1 },
			]
		};
}