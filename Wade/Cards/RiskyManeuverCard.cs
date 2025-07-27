using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class RiskyManeuverCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/RiskyManeuver.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "RiskyManeuver", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 1, artTint = "ffffff" };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 1 },
				new Odds.RollAction(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = true },
					new Odds.RollAction()
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
			],
			Upgrade.A => [
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 3 },
				new Odds.RollAction(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
				new Odds.RollAction(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
			],
			_ => [
				new AStatus { targetPlayer = true, status = Status.tempShield, statusAmount = 1 },
				new Odds.RollAction(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
				new Odds.RollAction(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
			],
		};
}