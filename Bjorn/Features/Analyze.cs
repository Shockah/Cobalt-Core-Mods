using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
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
	
	public static ACardSelect SetFilterAnalyzed(this ACardSelect self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterAnalyzed", value);
		return self;
	}
	
	public static ACardSelect SetFilterReevaluated(this ACardSelect self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterReevaluated", value);
		return self;
	}

	public static ACardSelect SetForceInclude(this ACardSelect self, int? cardId)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "ForceInclude", cardId);
		return self;
	}
}

internal static class AnalyzeCardExt
{
	public static bool IsAnalyzable(this Card card, State state)
	{
		if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, AnalyzeManager.AnalyzedTrait))
			return false;
		if (card.GetDataWithOverrides(state).temporary && !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, AnalyzeManager.ReevaluatedTrait))
			return false;
		return true;
	}
}

internal sealed class AnalyzeManager : IRegisterable
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Analyzed", "name"]).Localize,
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
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Reevaluated", "name"]).Localize,
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

		// TODO: make this a Nickel API
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Mod).Assembly.GetType("Nickel.CardTraitManager"), "SetCardTraitOverride"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardTraitManager_SetCardTraitOverride_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardTraitManager_SetCardTraitOverride_Postfix))
		);
	}

	public static List<Tooltip> GetAnalyzeTooltips(State state)
		=> [
			new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::Analyze")
			{
				Icon = AnalyzeIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "Analyze", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "Analyze", "description"]),
			},
			.. (AnalyzedTrait.Configuration.Tooltips?.Invoke(state, null) ?? []),
		];

	public static List<Tooltip> GetSelfAnalyzeTooltips(State state)
		=> [
			new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::SelfAnalyze")
			{
				Icon = SelfAnalyzeIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "SelfAnalyze", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "SelfAnalyze", "description"]),
			},
			.. (AnalyzedTrait.Configuration.Tooltips?.Invoke(state, null) ?? []),
		];

	public static List<Tooltip> GetAnalyzeOrSelfAnalyzeTooltips(State state)
		=> [
			new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::AnalyzeOrSelfAnalyze")
			{
				Icon = AnalyzeOrSelfAnalyzeIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "AnalyzeOrSelfAnalyze", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "AnalyzeOrSelfAnalyze", "description"]),
			},
			.. (AnalyzedTrait.Configuration.Tooltips?.Invoke(state, null) ?? []),
		];

	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;
		ModEntry.Instance.Helper.ModData.CopyOwnedModData(__instance, route);
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterAnalyzable") is { } filterAnalyzable)
		{
			for (var i = __result.Count - 1; i >= 0; i--)
			{
				if (__result[i].IsAnalyzable(g.state) == filterAnalyzable)
					continue;
				__result.RemoveAt(i);
			}
		}
		
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FitlerAnalyzed") is { } filterAnalyzed)
		{
			for (var i = __result.Count - 1; i >= 0; i--)
			{
				if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(g.state, __result[i], AnalyzedTrait) == filterAnalyzed)
					continue;
				__result.RemoveAt(i);
			}
		}
		
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FitlerReevaluated") is { } filterReevaluated)
		{
			for (var i = __result.Count - 1; i >= 0; i--)
			{
				if (ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(g.state, __result[i], ReevaluatedTrait) == filterReevaluated)
					continue;
				__result.RemoveAt(i);
			}
		}

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(__instance, "ForceInclude") is { } forceIncludeCardId)
		{
			var index = __result.FindIndex(card => card.uuid == forceIncludeCardId);
			if (index != -1)
				__result.RemoveAt(index);
			if (g.state.FindCard(forceIncludeCardId) is { } card)
				__result.Insert(0, card);
		}
	}

	private static bool Card_RenderAction_Prefix(G g, State state, CardAction action, bool dontDraw, int shardAvailable, int stunChargeAvailable, int bubbleJuiceAvailable, ref int __result)
	{
		if (action is AnalyzeCostAction analyzeCostAction)
		{
			var oldActionDisabled = analyzeCostAction.Action.disabled;
			analyzeCostAction.Action.disabled = analyzeCostAction.disabled;

			var position = g.Push(rect: new()).rect.xy;
			var initialX = (int)position.x;

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
			var oldActionDisabled = selfAnalyzeCostAction.Action.disabled;
			selfAnalyzeCostAction.Action.disabled = selfAnalyzeCostAction.disabled;

			var position = g.Push(rect: new()).rect.xy;
			var initialX = (int)position.x;

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

		if (action is OnAnalyzeAction onAnalyzeAction)
		{
			var oldActionDisabled = onAnalyzeAction.Action.disabled;
			onAnalyzeAction.Action.disabled = onAnalyzeAction.disabled;

			var position = g.Push(rect: new()).rect.xy;
			var initialX = (int)position.x;

			if (!dontDraw)
				Draw.Sprite(OnAnalyzeIcon.Sprite, position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
			position.x += 10;

			g.Push(rect: new(position.x - initialX, 0));
			position.x += Card.RenderAction(g, state, onAnalyzeAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			g.Pop();

			__result = (int)position.x - initialX;
			g.Pop();
			onAnalyzeAction.Action.disabled = oldActionDisabled;

			return false;
		}

		return true;
	}

	private static void CardTraitManager_SetCardTraitOverride_Prefix(Card card, ICardTraitEntry trait, bool? overrideValue, ref bool __state)
	{
		if (trait != AnalyzedTrait)
			return;
		if (overrideValue == false)
			return;
		__state = ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(MG.inst.g.state, card, trait);
	}

	private static void CardTraitManager_SetCardTraitOverride_Postfix(Card card, ICardTraitEntry trait, bool? overrideValue, ref bool __state)
	{
		if (trait != AnalyzedTrait)
			return;
		if (overrideValue == false)
			return;
		if (__state)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(MG.inst.g.state, card, trait))
			return;
		if (MG.inst.g.state.route is not Combat combat)
			return;

		var meta = card.GetMeta();
		if (MG.inst.g.state.CharacterIsMissing(meta.deck))
			return;

		combat.QueueImmediate(
			card.GetActionsOverridden(MG.inst.g.state, combat)
				.Where(action => !action.disabled)
				.OfType<OnAnalyzeAction>()
				.Select(triggerAction => triggerAction.Action)
				.Select(action =>
				{
					action.whoDidThis = meta.deck;
					return action;
				})
		);
	}
}

// TODO: register wrapped action
// TODO: pre-check if there are enough cards
internal sealed class AnalyzeCostAction : CardAction
{
	public required CardAction Action;
	public int Count = 1;
	public int? CardId;
	public bool Permanent;
	public bool? FilterAccelerated;
	public bool? FilterExhaust;
	public int? FilterMinCost;

	public override List<Tooltip> GetTooltips(State s)
		=> [
			.. CardId is null ? AnalyzeManager.GetAnalyzeTooltips(s) : AnalyzeManager.GetAnalyzeOrSelfAnalyzeTooltips(s),
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
			browseAction = new AnalyzeCostBrowseAction
			{
				Action = Action,
				ToAnalyzeLeft = Count - 1,
				Permanent = Permanent,
				FilterAccelerated = FilterAccelerated,
				FilterExhaust = FilterExhaust,
				FilterMinCost = FilterMinCost,
			},
			browseSource = CardBrowse.Source.Hand,
			filterExhaust = FilterExhaust,
			filterMinCost = FilterMinCost,
		}.SetFilterAnalyzable(true).SetFilterAccelerated(FilterAccelerated).SetForceInclude(CardId));
	}

	private sealed class AnalyzeCostBrowseAction : CardAction
	{
		public required CardAction Action;
		public required int ToAnalyzeLeft;
		public bool Permanent;
		public bool? FilterAccelerated;
		public bool? FilterExhaust;
		public int? FilterMinCost;

		public override string? GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["action", "Analyze", "uiTitle"]);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;

			if (selectedCard is null)
				return;

			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, selectedCard, AnalyzeManager.AnalyzedTrait, true, permanent: Permanent);

			if (ToAnalyzeLeft > 0)
			{
				c.QueueImmediate(new AnalyzeCostAction
				{
					Action = Action,
					Count = ToAnalyzeLeft,
					Permanent = Permanent,
					FilterAccelerated = FilterAccelerated,
					FilterExhaust = FilterExhaust,
					FilterMinCost = FilterMinCost,
				});
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
			.. AnalyzeManager.GetSelfAnalyzeTooltips(s),
			.. Action.GetTooltips(s),
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (s.FindCard(CardId) is not { } card || ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, AnalyzeManager.AnalyzedTrait))
		{
			timer = 0;
			return;
		}

		ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, AnalyzeManager.AnalyzedTrait, true, permanent: false);
		c.QueueImmediate(Action);
	}
}

internal sealed class OnAnalyzeAction : CardAction
{
	public required CardAction Action;

	public override Icon? GetIcon(State s)
		=> new(AnalyzeManager.OnAnalyzeIcon.Sprite, null, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::OnAnalyze")
			{
				Icon = AnalyzeManager.OnAnalyzeIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "OnAnalyze", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "OnAnalyze", "description"]),
			},
			.. (AnalyzeManager.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? []),
			.. Action.GetTooltips(s)
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
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
			.. AnalyzeManager.GetAnalyzeTooltips(s),
		];
}

internal sealed class AnalyzedCondition : IKokoroApi.IV2.IConditionalApi.IBoolExpression
{
	public required bool Analyzed;
	
	public bool GetValue(State state, Combat combat)
		=> Analyzed;

	public string GetTooltipDescription(State state, Combat? combat)
		=> ModEntry.Instance.Localizations.Localize(["condition", "Analyzed", "description"]);

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(
				AnalyzeManager.AnalyzedIcon.Sprite,
				position.x,
				position.y,
				color: isDisabled ? Colors.disabledIconTint : Colors.white
			);
		position.x += 8;
	}

	public IEnumerable<Tooltip> OverrideConditionalTooltip(State state, Combat? combat, Tooltip defaultTooltip, string defaultTooltipDescription)
		=> [
			new GlossaryTooltip($"AConditional::{ModEntry.Instance.Package.Manifest.UniqueName}::Analyzed")
			{
				Icon = AnalyzeManager.AnalyzedIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["condition", "Analyzed", "title"]),
				Description = defaultTooltipDescription,
			}
		];

	public IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat)
		=> [.. AnalyzeManager.AnalyzedTrait.Configuration.Tooltips?.Invoke(state, null) ?? []];
}