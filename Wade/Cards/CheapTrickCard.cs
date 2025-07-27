using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class CheapTrickCard : Card, IRegisterable
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
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/CheapTrick.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "CheapTrick", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, artTint = "ffffff" };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = true },
					new AAttack { damage = GetDmg(s, 1) }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(!s.EnumerateAllArtifacts().Any(a => a is PressedCloverArtifact)).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 2 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(!s.EnumerateAllArtifacts().Any(a => a is PressedCloverArtifact)).AsCardAction,
			],
			Upgrade.A => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = true },
					new AAttack { damage = GetDmg(s, 2) }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(!s.EnumerateAllArtifacts().Any(a => a is PressedCloverArtifact)).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(!s.EnumerateAllArtifacts().Any(a => a is PressedCloverArtifact)).AsCardAction,
			],
			_ => [
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = true },
					new AAttack { damage = GetDmg(s, 1) }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(!s.EnumerateAllArtifacts().Any(a => a is PressedCloverArtifact)).AsCardAction,
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 }
				).SetShowQuestionMark(false).SetFadeUnsatisfied(!s.EnumerateAllArtifacts().Any(a => a is PressedCloverArtifact)).AsCardAction,
			],
		};
}