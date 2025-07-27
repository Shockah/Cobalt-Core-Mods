using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Wade;

internal sealed class SpareDiceCard : Card, IRegisterable
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
				dontOffer = true,
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Card/SpareDice.png"), StableSpr.cards_colorless).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "SpareDice", "name"]).Localize,
		});
	}

	public override CardData GetData(State state)
		=> new() { cost = 0, temporary = true, exhaust = true, retain = true, artTint = "ffffff" };

	public override List<CardAction> GetActions(State s, Combat c)
		=> upgrade switch
		{
			Upgrade.B => [
				new Odds.RollAction(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = false },
					new Odds.RollAction()
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
			],
			Upgrade.A => [
				new Odds.RollAction(),
				ModEntry.Instance.KokoroApi.Conditional.MakeAction(
					new Odds.TrendCondition { Positive = true },
					new Odds.RollAction()
				).SetShowQuestionMark(false).SetFadeUnsatisfied(false).AsCardAction,
			],
			_ => [
				new Odds.RollAction(),
			],
		};
}