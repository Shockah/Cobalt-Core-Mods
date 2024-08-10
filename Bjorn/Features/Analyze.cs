using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bjorn;

internal static class AnalyzeCardSelectFiltersExt
{
	public static ACardSelect SetFilterAnalyzable(this ACardSelect self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterAnalyzable", value);
		return self;
	}
}

internal static class AnalyzeCardExt
{
	public static bool IsAnalyzable(this Card card, State state)
	{
		if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Analyze.AnalyzedTrait))
			return false;
		if (card.GetDataWithOverrides(state).temporary && !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Analyze.ReevaluatedTrait))
			return false;
		return true;
	}
}

internal sealed class Analyze : IRegisterable
{
	internal static ISpriteEntry AnalyzeIcon { get; private set; } = null!;
	internal static ISpriteEntry Analyze2Icon { get; private set; } = null!;

	internal static ICardTraitEntry AnalyzedTrait { get; private set; } = null!;
	internal static ICardTraitEntry ReevaluatedTrait { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		AnalyzeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Analyze.png"));
		Analyze2Icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Analyze2.png"));

		AnalyzedTrait = helper.Content.Cards.RegisterTrait("Analyzed", new()
		{
			Icon = (_, _) => AnalyzeIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Spontaneous"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{package.Manifest.UniqueName}::Analyzed")
				{
					Icon = AnalyzeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Analyzed", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Analyzed", "description"]),
				}
			]
		});

		ReevaluatedTrait = helper.Content.Cards.RegisterTrait("Reevaluated", new()
		{
			Icon = (_, _) => AnalyzeIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Reevaluated"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{package.Manifest.UniqueName}::Reevaluated")
				{
					Icon = AnalyzeIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Reevaluated", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Reevaluated", "description"]),
				}
			]
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
	}

	public static Tooltip GetAnalyzeTooltip()
		=> new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::Analyze")
		{
			Icon = AnalyzeIcon.Sprite,
			TitleColor = Colors.action,
			Title = ModEntry.Instance.Localizations.Localize(["action", "Analyze", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["action", "Analyze", "description"]),
		};

	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterAnalyzable") is { } filterAnalyzable)
			ModEntry.Instance.Helper.ModData.SetModData(route, "FilterAnalyzable", filterAnalyzable);
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterAnalyzable") is not { } filterAnalyzable)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
		{
			if (__result[i].IsAnalyzable(g.state) == filterAnalyzable)
				continue;
			__result.RemoveAt(i);
		}
	}
}

// TODO: register wrapped action
// TODO: pre-check if there are enough cards
internal sealed class AnalyzeCostAction : CardAction
{
	public required List<CardAction> Actions;
	public int Cards = 1;

	public override List<Tooltip> GetTooltips(State s)
		=> [
			Analyze.GetAnalyzeTooltip(),
			.. (Analyze.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? []),
			.. Actions.SelectMany(a => a.GetTooltips(s))
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Cards <= 0)
		{
			c.QueueImmediate(Actions);
			return;
		}

		c.QueueImmediate(new ACardSelect
		{
			browseAction = new AnalyzeCostBrowseAction { Actions = Actions, ToAnalyzeLeft = Cards - 1 },
			browseSource = CardBrowse.Source.Hand,
		}.SetFilterAnalyzable(true));
	}

	// TODO: register wrapped action
	private sealed class AnalyzeCostBrowseAction : CardAction
	{
		public required List<CardAction> Actions;
		public required int ToAnalyzeLeft;

		public override string? GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["action", "Analyze", "uiTitle"]);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (selectedCard is null)
				return;

			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, Analyze.AnalyzedTrait, true, permanent: false);

			if (ToAnalyzeLeft > 0)
			{
				c.QueueImmediate(new AnalyzeCostAction { Actions = Actions, Cards = ToAnalyzeLeft });
				return;
			}

			foreach (var action in Actions)
				action.selectedCard = selectedCard;
			c.QueueImmediate(Actions);
		}
	}
}

internal sealed class SelfAnalyzeCostAction : CardAction
{
	public required int CardId;
	public required List<CardAction> Actions;

	public override List<Tooltip> GetTooltips(State s)
		=> Actions.SelectMany(a => a.GetTooltips(s)).ToList();

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (s.FindCard(CardId) is not { } card)
		{
			timer = 0;
			return;
		}

		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, Analyze.AnalyzedTrait, true, permanent: false);
		c.QueueImmediate(Actions);
	}
}

internal sealed class AnalyzableVariableHint : AVariableHint
{
	public required int CardId;

	public AnalyzableVariableHint()
	{
		this.hand = true;
	}

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action::{ModEntry.Instance.Package.Manifest.UniqueName}::AnalyzableVariableHint.desc")
			{
				Description = ModEntry.Instance.Localizations.Localize(["x", "Analyzable", s.route is Combat ? "stateful" : "stateless"], new { Count = (s.route as Combat)?.hand.Count(card => card.uuid != CardId && card.IsAnalyzable(s)) })
			},
			Analyze.GetAnalyzeTooltip(),
			.. (Analyze.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? []),
		];
}