using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class WeighTheOddsCard : Card, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.WadeDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "WeighTheOdds", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> upgrade switch
		{
			Upgrade.A => new() { cost = 0, exhaust = true, floppable = true, artTint = "ffffff", art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
			_ => new() { cost = 1, exhaust = true, floppable = true, artTint = "ffffff", art = flipped ? StableSpr.cards_Adaptability_Bottom : StableSpr.cards_Adaptability_Top },
		};

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Odds.GreenTrendStatus.Status, statusAmount = 3, disabled = flipped },
				new AStatus { targetPlayer = true, status = Odds.RedTrendStatus.Status, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = true, status = Odds.GreenTrendStatus.Status, statusAmount = 1, disabled = !flipped },
				new AStatus { targetPlayer = true, status = Odds.RedTrendStatus.Status, statusAmount = 3, disabled = !flipped },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Odds.GreenTrendStatus.Status, statusAmount = 2, disabled = flipped },
				new AStatus { targetPlayer = true, status = Odds.RedTrendStatus.Status, statusAmount = 1, disabled = flipped },
				new ADummyAction(),
				new AStatus { targetPlayer = true, status = Odds.GreenTrendStatus.Status, statusAmount = 1, disabled = !flipped },
				new AStatus { targetPlayer = true, status = Odds.RedTrendStatus.Status, statusAmount = 2, disabled = !flipped },
			],
		};
}