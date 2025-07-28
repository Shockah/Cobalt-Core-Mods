using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class LuckOfTheDrawCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/LuckOfTheDraw.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "LuckOfTheDraw", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1, artTint = "ffffff" };

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var realOdds = s.ship.Get(Odds.OddsStatus.Status);
		if (upgrade == Upgrade.A)
		{
			realOdds += 1 + s.ship.Get(Status.boost);
			if (realOdds == 0)
				realOdds++;
		}

		return upgrade switch
		{
			Upgrade.B => [
				new Odds.OddsVariableHint(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = true },
					ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
						new ADrawCard { count = ModEntry.Instance.Api.GetKnownOdds(s, c) is null ? 0 : realOdds * 2, xHint = 2 },
						new ADrawCard { count = realOdds * 2, xHint = 2 }
					).AsCardAction
				).SetShowQuestionMark(false).SetFadeUnsatisfied(ModEntry.Instance.Api.GetKnownOdds(s, c) is not null).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Odds.GreenTrendStatus.Status, statusAmount = 1 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(ModEntry.Instance.Api.GetKnownOdds(s, c) is not null).AsCardAction,
				new Odds.RollAction(),
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Odds.OddsStatus.Status, statusAmount = 1 },
				new Odds.OddsVariableHint(),
				ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new Odds.TrendCondition { Positive = true, OverrideValue = realOdds > 0 },
						ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
							new ADrawCard { count = ModEntry.Instance.Api.GetKnownOdds(s, c) is null ? 0 : realOdds * 2, xHint = 2 },
							new ADrawCard { count = realOdds * 2, xHint = 2 }
						).AsCardAction
					).SetShowQuestionMark(false).SetFadeUnsatisfied(ModEntry.Instance.Api.GetKnownOdds(s, c) is not null).AsCardAction,
					ModEntry.Instance.KokoroApi.Conditional.MakeAction(
						new Odds.TrendCondition { Positive = true },
						ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
							new ADrawCard { count = ModEntry.Instance.Api.GetKnownOdds(s, c) is null ? 0 : realOdds * 2, xHint = 2 },
							new ADrawCard { count = realOdds * 2, xHint = 2 }
						).AsCardAction
					).SetShowQuestionMark(false).SetFadeUnsatisfied(ModEntry.Instance.Api.GetKnownOdds(s, c) is not null).AsCardAction
				).AsCardAction,
				new Odds.RollAction(),
			],
			_ => [
				new Odds.OddsVariableHint(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = true },
					ModEntry.Instance.KokoroApi.SpoofedActions.MakeAction(
						new ADrawCard { count = ModEntry.Instance.Api.GetKnownOdds(s, c) is null ? 0 : realOdds * 2, xHint = 2 },
						new ADrawCard { count = realOdds * 2, xHint = 2 }
					).AsCardAction
				).SetShowQuestionMark(false).SetFadeUnsatisfied(ModEntry.Instance.Api.GetKnownOdds(s, c) is not null).AsCardAction,
				new Odds.RollAction(),
			]
		};
	}
}