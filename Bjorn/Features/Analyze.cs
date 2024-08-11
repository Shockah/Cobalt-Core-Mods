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
	internal static ISpriteEntry SelfAnalyzeIcon { get; private set; } = null!;
	internal static ISpriteEntry AnalyzeOrSelfAnalyzeIcon { get; private set; } = null!;
	internal static ISpriteEntry OnAnalyzeIcon { get; private set; } = null!;
	internal static ISpriteEntry AnalyzedIcon { get; private set; } = null!;
	internal static ISpriteEntry ReevaluatedIcon { get; private set; } = null!;

	internal static ICardTraitEntry AnalyzedTrait { get; private set; } = null!;
	internal static ICardTraitEntry ReevaluatedTrait { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		AnalyzeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Analyze.png"));
		SelfAnalyzeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/AnalyzeSelf.png"));
		AnalyzeOrSelfAnalyzeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/AnalyzeOrAnalyzeSelf.png"));
		OnAnalyzeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/OnAnalyze.png"));
		AnalyzedIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Analyzed.png"));
		ReevaluatedIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Reevaluated.png"));

		AnalyzedTrait = helper.Content.Cards.RegisterTrait("Analyzed", new()
		{
			Icon = (_, _) => AnalyzedIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Spontaneous"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{package.Manifest.UniqueName}::Analyzed")
				{
					Icon = AnalyzedIcon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Analyzed", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Analyzed", "description"]),
				}
			]
		});

		ReevaluatedTrait = helper.Content.Cards.RegisterTrait("Reevaluated", new()
		{
			Icon = (_, _) => ReevaluatedIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Reevaluated"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{package.Manifest.UniqueName}::Reevaluated")
				{
					Icon = ReevaluatedIcon.Sprite,
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
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
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

	public static Tooltip GetSelfAnalyzeTooltip()
		=> new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::SelfAnalyze")
		{
			Icon = SelfAnalyzeIcon.Sprite,
			TitleColor = Colors.action,
			Title = ModEntry.Instance.Localizations.Localize(["action", "SelfAnalyze", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["action", "SelfAnalyze", "description"]),
		};

	public static Tooltip GetAnalyzeOrSelfAnalyzeTooltip()
		=> new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::AnalyzeOrSelfAnalyze")
		{
			Icon = AnalyzeOrSelfAnalyzeIcon.Sprite,
			TitleColor = Colors.action,
			Title = ModEntry.Instance.Localizations.Localize(["action", "AnalyzeOrSelfAnalyze", "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["action", "AnalyzeOrSelfAnalyze", "description"]),
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

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is AnalyzeCostAction analyzeCostAction)
		{
			bool oldActionDisabled = analyzeCostAction.Action.disabled;
			analyzeCostAction.Action.disabled = analyzeCostAction.disabled;

			var position = g.Push(rect: new()).rect.xy;
			int initialX = (int)position.x;

			if (!dontDraw)
				Draw.Sprite((analyzeCostAction.CardId is null ? AnalyzeIcon : AnalyzeOrSelfAnalyzeIcon).Sprite, position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
			position.x += 9;

			if (!dontDraw)
				BigNumbers.Render(analyzeCostAction.Count, position.x, position.y, action.disabled ? Colors.disabledText : Colors.textMain);
			position.x += analyzeCostAction.Count.ToString().Length * 6;

			position.x += 2;

			g.Push(rect: new(position.x - initialX, 0));
			position.x += Card.RenderAction(g, state, analyzeCostAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			g.Pop();

			__result = (int)position.x - initialX;
			g.Pop();
			analyzeCostAction.Action.disabled = oldActionDisabled;

			return false;
		}
		
		if (action is SelfAnalyzeCostAction selfAnalyzeCostAction)
		{
			bool oldActionDisabled = selfAnalyzeCostAction.Action.disabled;
			selfAnalyzeCostAction.Action.disabled = selfAnalyzeCostAction.disabled;

			var position = g.Push(rect: new()).rect.xy;
			int initialX = (int)position.x;

			if (!dontDraw)
				Draw.Sprite(SelfAnalyzeIcon.Sprite, position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
			position.x += 10;

			g.Push(rect: new(position.x - initialX, 0));
			position.x += Card.RenderAction(g, state, selfAnalyzeCostAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			g.Pop();

			__result = (int)position.x - initialX;
			g.Pop();
			selfAnalyzeCostAction.Action.disabled = oldActionDisabled;

			return false;
		}

		return true;
	}
}

// TODO: register wrapped action
// TODO: pre-check if there are enough cards
internal sealed class AnalyzeCostAction : CardAction
{
	public required CardAction Action;
	public int Count = 1;
	public int? CardId;

	public override List<Tooltip> GetTooltips(State s)
		=> [
			CardId is null ? Analyze.GetAnalyzeTooltip() : Analyze.GetAnalyzeOrSelfAnalyzeTooltip(),
			.. (Analyze.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? []),
			.. Action.GetTooltips(s),
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Count <= 0)
		{
			c.QueueImmediate(Action);
			return;
		}

		c.QueueImmediate(new ACardSelect
		{
			browseAction = new AnalyzeCostBrowseAction { Action = Action, ToAnalyzeLeft = Count - 1 },
			browseSource = CardBrowse.Source.Hand,
		}.SetFilterAnalyzable(true));
	}

	// TODO: register wrapped action
	private sealed class AnalyzeCostBrowseAction : CardAction
	{
		public required CardAction Action;
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
				c.QueueImmediate(new AnalyzeCostAction { Action = Action, Count = ToAnalyzeLeft });
				return;
			}

			Action.selectedCard = selectedCard;
			c.QueueImmediate(Action);
		}
	}
}

// TODO: register wrapped action
internal sealed class SelfAnalyzeCostAction : CardAction
{
	public required int CardId;
	public required CardAction Action;

	public override List<Tooltip> GetTooltips(State s)
		=> [
			Analyze.GetSelfAnalyzeTooltip(),
			.. (Analyze.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? []),
			.. Action.GetTooltips(s),
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (s.FindCard(CardId) is not { } card || ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Analyze.AnalyzedTrait))
		{
			timer = 0;
			return;
		}

		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, Analyze.AnalyzedTrait, true, permanent: false);
		c.QueueImmediate(Action);
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