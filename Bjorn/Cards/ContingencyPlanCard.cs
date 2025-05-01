using System.Collections.Generic;
using System.Reflection;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class ContingencyPlanCard : Card, IRegisterable, IHasCustomCardTraits
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		var entry = helper.Content.Cards.RegisterCard(MethodBase.GetCurrentMethod()!.DeclaringType!.Name, new()
		{
			CardType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				deck = ModEntry.Instance.BjornDeck.Deck,
				rarity = ModEntry.GetCardRarity(MethodBase.GetCurrentMethod()!.DeclaringType!),
				upgradesTo = [Upgrade.A, Upgrade.B],
			},
			Art = helper.Content.Sprites.RegisterSpriteOrDefault(package.PackageRoot.GetRelativeFile("assets/Cards/ContingencyPlan.png"), StableSpr.cards_dizzy).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["card", "ContingencyPlan", "name"]).Localize,
		});
		
		ModEntry.Instance.KokoroApi.Limited.SetBaseLimitedUses(entry.UniqueName, 3);
	}

	public IReadOnlySet<ICardTraitEntry> GetInnateTraits(State state)
		=> new HashSet<ICardTraitEntry> { ModEntry.Instance.KokoroApi.Limited.Trait };

	public override CardData GetData(State state)
	{
		var description = ModEntry.Instance.Localizations.Localize(["card", "ContingencyPlan", "description", upgrade.ToString(), flipped ? "flipped" : "normal"]);
		var analyzed = ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, this, AnalyzeManager.AnalyzedTrait);
		return upgrade.Switch<CardData>(
			() => new() { cost = 1, floppable = !analyzed, description = description },
			() => new() { cost = 1, floppable = !analyzed, retain = true, description = description },
			() => new() { cost = 2, floppable = !analyzed, description = description }
		);
	}

	public override List<CardAction> GetActions(State s, Combat c)
	{
		var analyzed = ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, this, AnalyzeManager.AnalyzedTrait);
		return upgrade.Switch<List<CardAction>>(
			none: () =>
			[
				new SmartShieldAction { TargetPlayer = true, Amount = analyzed ? 3 : 2, disabled = flipped },
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = analyzed ? 2 : 1, disabled = !flipped },
			],
			a: () =>
			[
				new SmartShieldAction { TargetPlayer = true, Amount = analyzed ? 3 : 2, disabled = flipped },
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = analyzed ? 2 : 1, disabled = !flipped },
			],
			b: () =>
			[
				new SmartShieldAction { TargetPlayer = true, Amount = analyzed ? 5 : 4, disabled = flipped },
				new AStatus { targetPlayer = true, status = RelativityManager.RelativityStatus.Status, statusAmount = analyzed ? 3 : 2, disabled = !flipped },
			]
		);
	}
}
