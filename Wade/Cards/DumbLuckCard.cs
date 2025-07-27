using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class DumbLuckCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/DumbLuck.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "DumbLuck", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, artTint = "ffffff" };

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var realOdds = s.ship.Get(Odds.OddsStatus.Status);
		realOdds += upgrade switch
		{
			Upgrade.B => 3,
			Upgrade.A => 1,
			_ => 2,
		} + s.ship.Get(Status.boost);
		if (realOdds == 0)
			realOdds++;

		return upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Odds.OddsStatus.Status, statusAmount = 3 },
				ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new Odds.TrendCondition { Positive = false, OverrideValue = realOdds > 0 },
						new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 3 }
					).SetShowQuestionMark(false).AsCardAction,
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new Odds.TrendCondition { Positive = false },
						new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 3 }
					).SetShowQuestionMark(false).AsCardAction
				).AsCardAction,
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Odds.OddsStatus.Status, statusAmount = 1 },
				ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new Odds.TrendCondition { Positive = false, OverrideValue = realOdds > 0 },
						new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 }
					).SetShowQuestionMark(false).AsCardAction,
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new Odds.TrendCondition { Positive = false },
						new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 }
					).SetShowQuestionMark(false).AsCardAction
				).AsCardAction,
				new AStatus { targetPlayer = true, status = Odds.OddsStatus.Status, statusAmount = 1 },
			],
			_ => [
				new AStatus { targetPlayer = true, status = Odds.OddsStatus.Status, statusAmount = 2 },
				ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new Odds.TrendCondition { Positive = false, OverrideValue = realOdds > 0 },
						new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 }
					).SetShowQuestionMark(false).AsCardAction,
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new Odds.TrendCondition { Positive = false },
						new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 }
					).SetShowQuestionMark(false).AsCardAction
				).AsCardAction,
			]
		};
	}
}