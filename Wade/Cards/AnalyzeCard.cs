using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class AnalyzeCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/Analyze.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "Analyze", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, artTint = "ffffff" };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new ADrawCard { count = 3 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(!s.EnumerateAllArtifacts().Any(a => a is PressedCloverArtifact)).AsCardAction,
				new Odds.RollAction(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new ADrawCard { count = 2 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
				new Odds.RollAction(),
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new ADrawCard { count = 4 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(!s.EnumerateAllArtifacts().Any(a => a is PressedCloverArtifact)).AsCardAction,
				new Odds.RollAction(),
			],
			_ => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new ADrawCard { count = 3 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(!s.EnumerateAllArtifacts().Any(a => a is PressedCloverArtifact)).AsCardAction,
				new Odds.RollAction(),
			],
		};
}