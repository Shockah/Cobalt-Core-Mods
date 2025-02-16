using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
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
	
	public static CardBrowse SetFilterAnalyzable(this CardBrowse self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterAnalyzable", value);
		return self;
	}
	
	public static ACardSelect SetFilterAnalyzed(this ACardSelect self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterAnalyzed", value);
		return self;
	}
	
	public static CardBrowse SetFilterAnalyzed(this CardBrowse self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterAnalyzed", value);
		return self;
	}
}

internal static class AnalyzeCardExt
{
	public static bool IsAnalyzable(this Card card, State state, Combat combat)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.CanAnalyzeArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;

			foreach (var hook in ModEntry.Instance.FilterIsCanAnalyzeHookEnabled(state, combat, card, ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts())))
				if (hook.CanAnalyze(args))
					return true;
			return false;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}
}

internal sealed class AnalyzeManager : IRegisterable
{
	internal static ISpriteEntry AnalyzeIcon { get; private set; } = null!;
	internal static ISpriteEntry SelfAnalyzeIcon { get; private set; } = null!;
	internal static ISpriteEntry AnalyzeOrSelfAnalyzeIcon { get; private set; } = null!;
	internal static ISpriteEntry OnAnalyzeIcon { get; private set; } = null!;
	internal static ISpriteEntry AnalyzedIcon { get; private set; } = null!;

	internal static ICardTraitEntry AnalyzedTrait { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		AnalyzeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Analyze.png"));
		SelfAnalyzeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/AnalyzeSelf.png"));
		AnalyzeOrSelfAnalyzeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/AnalyzeOrAnalyzeSelf.png"));
		OnAnalyzeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/OnAnalyze.png"));
		AnalyzedIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Analyzed.png"));

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

		ModEntry.Instance.Helper.Content.Cards.OnSetCardTraitOverride += (_, e) =>
		{
			if (e.CardTrait != AnalyzedTrait)
				return;
			if (e.State.route is not Combat combat)
				return;
			
			var meta = e.Card.GetMeta();
			if (MG.inst.g.state.CharacterIsMissing(meta.deck))
				return;

			combat.QueueImmediate(
				e.Card.GetActionsOverridden(MG.inst.g.state, combat)
					.Where(action => !action.disabled)
					.OfType<OnAnalyzeAction>()
					.Select(triggerAction => triggerAction.Action)
					.Select(action =>
					{
						action.whoDidThis = meta.deck;
						return action;
					})
			);
		};
		
		ModEntry.Instance.HookManager.Register(NonAnalyzedNonTempCardsAnalyzeHook.Instance, 0);
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

	public static List<Tooltip> GetDeanalyzeTooltips(State state)
		=> [
			new GlossaryTooltip($"action.{ModEntry.Instance.Package.Manifest.UniqueName}::Deanalyze")
			{
				Icon = AnalyzeIcon.Sprite,
				TitleColor = Colors.action,
				Title = ModEntry.Instance.Localizations.Localize(["action", "Deanalyze", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["action", "Deanalyze", "description"]),
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
			.. AnalyzedTrait.Configuration.Tooltips?.Invoke(state, null) ?? [],
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
			.. AnalyzedTrait.Configuration.Tooltips?.Invoke(state, null) ?? [],
			.. GetAnalyzeTooltips(state),
		];
	
	public static void OnCardsAnalyzed(State state, Combat combat, IReadOnlyList<Card> cards)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.OnCardsAnalyzedArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Cards = cards;

			foreach (var hook in ModEntry.Instance.HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				hook.OnCardsAnalyzed(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;
		ModEntry.Instance.Helper.ModData.CopyOwnedModData(__instance, route);
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterAnalyzable") is { } filterAnalyzable && g.state.route is Combat combat)
		{
			for (var i = __result.Count - 1; i >= 0; i--)
			{
				if (__result[i].IsAnalyzable(g.state, combat) == filterAnalyzable)
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

			var count = analyzeCostAction.Count;
			if (!dontDraw)
				BigNumbers.Render(count, position.x, position.y, action.disabled ? Colors.disabledText : Colors.textMain);
			position.x += count.ToString().Length * 6;

			position.x += 2;

			g.Push(rect: new(position.x - initialX));
			position.x += Card.RenderAction(g, state, analyzeCostAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			g.Pop();

			__result = (int)position.x - initialX;
			g.Pop();
			analyzeCostAction.Action.disabled = oldActionDisabled;

			return false;
		}

		if (action is SelfAnalyzeCostAction selfAnalyzeCostAction)
		{
			var renderAsDisabled = action.disabled || (state.route is Combat combat && state.FindCard(selfAnalyzeCostAction.CardId) is { } card && !card.IsAnalyzable(state, combat));
			var oldActionDisabled = selfAnalyzeCostAction.Action.disabled;
			selfAnalyzeCostAction.Action.disabled = renderAsDisabled;

			var position = g.Push(rect: new()).rect.xy;
			var initialX = (int)position.x;

			if (!dontDraw)
				Draw.Sprite(SelfAnalyzeIcon.Sprite, position.x, position.y, color: renderAsDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += 10;

			g.Push(rect: new(position.x - initialX));
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

			g.Push(rect: new(position.x - initialX));
			position.x += Card.RenderAction(g, state, onAnalyzeAction.Action, dontDraw, shardAvailable, stunChargeAvailable, bubbleJuiceAvailable);
			g.Pop();

			__result = (int)position.x - initialX;
			g.Pop();
			onAnalyzeAction.Action.disabled = oldActionDisabled;

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
	public int MinCount = 1;
	public int MaxCount = 1;
	public int? CardId;
	public bool Permanent;
	public bool Deanalyze;
	public bool? FilterAccelerated;
	public bool? FilterExhaust;

	[JsonIgnore]
	public int Count
	{
		get => MaxCount;
		init
		{
			MinCount = value;
			MaxCount = value;
		}
	}

	public override List<Tooltip> GetTooltips(State s)
		=> [
			.. Deanalyze ? AnalyzeManager.GetDeanalyzeTooltips(s) : (CardId is null ? AnalyzeManager.GetAnalyzeTooltips(s) : AnalyzeManager.GetAnalyzeOrSelfAnalyzeTooltips(s)),
			.. Action.GetTooltips(s),
		];

	public override Route? BeginWithRoute(G g, State s, Combat c)
	{
		var card = CardId is { } cardId ? s.FindCard(cardId) : null;
		var cardCanPay = card is not null && (Deanalyze ? ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, AnalyzeManager.AnalyzedTrait) : card.IsAnalyzable(s, c));
		var cardIsEnough = cardCanPay && MaxCount >= 1;

		if (card is not null && cardIsEnough)
		{
			ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, AnalyzeManager.AnalyzedTrait, !Deanalyze, permanent: false);
			if (Permanent)
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, AnalyzeManager.AnalyzedTrait, !Deanalyze, permanent: true);
			
			Action.selectedCard = card;
			c.QueueImmediate(Action);
			
			if (!Deanalyze)
				AnalyzeManager.OnCardsAnalyzed(s, c, [card]);

			return null;
		}
		
		var baseRoute = new CardBrowse
		{
			browseAction = new AnalyzeCostBrowseAction
			{
				Action = Action,
				RequiredCount = MinCount,
				Permanent = Permanent,
				Deanalyze = Deanalyze,
				CardIdToIncludeAsCost = cardCanPay ? CardId : null,
			},
			browseSource = CardBrowse.Source.Hand,
			filterExhaust = FilterExhaust,
			allowCancel = true,
			filterUUID = CardId,
		}.SetFilterAnalyzable(Deanalyze ? null : true).SetFilterAnalyzed(Deanalyze).SetFilterAccelerated(FilterAccelerated);
		
		if (MaxCount <= (cardCanPay ? 2 : 1))
			return baseRoute;

		baseRoute.allowCancel = false;
		return ModEntry.Instance.KokoroApi.MultiCardBrowse.MakeRoute(baseRoute)
			.SetMinSelected(0)
			.SetMaxSelected(MaxCount)
			.AsRoute;
	}

	private sealed class AnalyzeCostBrowseAction : CardAction
	{
		public required CardAction Action;
		public required int RequiredCount;
		public bool Permanent;
		public bool Deanalyze;
		public int? CardIdToIncludeAsCost;

		public override string GetCardSelectText(State s)
			=> ModEntry.Instance.Localizations.Localize(["action", Deanalyze ? "Deanalyze" : "Analyze", "uiTitle"]);

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			
			var selectedCards = ModEntry.Instance.KokoroApi.MultiCardBrowse.GetSelectedCards(this)?.ToList() ?? (selectedCard is null ? [] : [selectedCard]);
			if (CardIdToIncludeAsCost is { } cardIdToIncludeAsCost && s.FindCard(cardIdToIncludeAsCost) is { } cardToIncludeAsCost)
				selectedCards.Insert(0, cardToIncludeAsCost);

			if (selectedCards.Count < RequiredCount)
				return;
			
			foreach (var card in selectedCards)
				ModEntry.Instance.Helper.Content.Cards.SetCardTraitOverride(s, card, AnalyzeManager.AnalyzedTrait, !Deanalyze, permanent: Permanent);
			
			switch (selectedCards.Count)
			{
				case 1:
					Action.selectedCard = selectedCards[0];
					break;
				case >= 2:
					ModEntry.Instance.KokoroApi.MultiCardBrowse.SetSelectedCards(Action, selectedCards);
					break;
			}
				
			c.QueueImmediate(Action);
			if (!Deanalyze)
				AnalyzeManager.OnCardsAnalyzed(s, c, selectedCards);
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
		AnalyzeManager.OnCardsAnalyzed(s, c, [card]);
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
			.. AnalyzeManager.AnalyzedTrait.Configuration.Tooltips?.Invoke(s, null) ?? [],
			.. Action.GetTooltips(s)
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
	}
}

internal sealed class AnalyzedInHandVariableHint : AVariableHint
{
	public int? IgnoreCardId;

	public AnalyzedInHandVariableHint()
	{
		this.hand = true;
	}

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action::{ModEntry.Instance.Package.Manifest.UniqueName}::AnalyzedInHandVariableHint.desc")
			{
				Description = ModEntry.Instance.Localizations.Localize(
					["x", "AnalyzedInHand", s.route is Combat ? "stateful" : "stateless"],
					new { Count = (s.route as Combat)?.hand.Count(card => card.uuid != IgnoreCardId && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, AnalyzeManager.AnalyzedTrait)) ?? 0 }
				)
			},
			.. AnalyzeManager.GetAnalyzeTooltips(s),
		];
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
				Description = ModEntry.Instance.Localizations.Localize(
					["x", "Analyzable", s.route is Combat ? "stateful" : "stateless"],
					new { Count = s.route is Combat combat ? combat.hand.Count(card => card.uuid != CardId && card.IsAnalyzable(s, combat)) : 0 }
				)
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

internal sealed class NonAnalyzedNonTempCardsAnalyzeHook : IBjornApi.IHook
{
	public static readonly NonAnalyzedNonTempCardsAnalyzeHook Instance = new();
	
	public bool CanAnalyze(IBjornApi.IHook.ICanAnalyzeArgs args)
		=> !args.Card.GetDataWithOverrides(args.State).temporary && !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(args.State, args.Card, AnalyzeManager.AnalyzedTrait);
}