using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.ILimitedApi Limited { get; } = new LimitedApi();
		
		public sealed class LimitedApi : IKokoroApi.IV2.ILimitedApi
		{
			public ICardTraitEntry Trait
				=> LimitedManager.Trait;

			public int DefaultLimitedUses
				=> LimitedManager.DefaultLimitedUses;

			public int GetBaseLimitedUses(string key, Upgrade upgrade)
				=> LimitedManager.GetBaseLimitedUses(key, upgrade);

			public void SetBaseLimitedUses(string key, int value)
				=> LimitedManager.SetBaseLimitedUses(key, value);

			public void SetBaseLimitedUses(string key, Upgrade upgrade, int value)
				=> LimitedManager.SetBaseLimitedUses(key, upgrade, value);

			public int GetStartingLimitedUses(State state, Card card)
				=> LimitedManager.GetStartingLimitedUses(state, card);

			public int GetLimitedUses(State state, Card card)
				=> LimitedManager.GetLimitedUses(state, card);

			public void SetLimitedUses(State state, Card card, int value)
				=> LimitedManager.SetLimitedUses(state, card, value);

			public void ResetLimitedUses(State state, Card card)
				=> LimitedManager.ResetLimitedUses(state, card);

			public IKokoroApi.IV2.ILimitedApi.IVariableHint? AsVariableHint(AVariableHint action)
				=> action as IKokoroApi.IV2.ILimitedApi.IVariableHint;

			public IKokoroApi.IV2.ILimitedApi.IVariableHint MakeVariableHint(int cardId)
				=> new LimitedUsesVariableHint { CardId = cardId };

			public IKokoroApi.IV2.ILimitedApi.IChangeLimitedUsesAction? AsChangeLimitedUsesAction(CardAction action)
				=> action as IKokoroApi.IV2.ILimitedApi.IChangeLimitedUsesAction;

			public IKokoroApi.IV2.ILimitedApi.IChangeLimitedUsesAction MakeChangeLimitedUsesAction(int cardId, int amount, AStatusMode mode = AStatusMode.Add)
				=> new ChangeLimitedUsesAction { CardId = cardId, Amount = amount, Mode = mode };

			public IKokoroApi.IV2.ILimitedApi.ICardSelect ModifyCardSelect(ACardSelect action)
				=> new CardSelectWrapper { Wrapped = action };

			public IKokoroApi.IV2.ILimitedApi.ICardBrowse ModifyCardBrowse(CardBrowse route)
				=> new CardBrowseWrapper { Wrapped = route };

			public void RegisterHook(IKokoroApi.IV2.ILimitedApi.IHook hook, double priority = 0)
				=> LimitedManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.ILimitedApi.IHook hook)
				=> LimitedManager.Instance.Unregister(hook);
			
			private sealed class CardSelectWrapper : IKokoroApi.IV2.ILimitedApi.ICardSelect
			{
				public required ACardSelect Wrapped { get; init; }

				public bool? FilterLimited
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterLimited");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterLimited", value);
				}

				[JsonIgnore]
				public ACardSelect AsCardAction
					=> Wrapped;
				
				public IKokoroApi.IV2.ILimitedApi.ICardSelect SetFilterLimited(bool? value)
				{
					FilterLimited = value;
					return this;
				}
			}
			
			private sealed class CardBrowseWrapper : IKokoroApi.IV2.ILimitedApi.ICardBrowse
			{
				public required CardBrowse Wrapped { get; init; }

				public bool? FilterLimited
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterLimited");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterLimited", value);
				}

				public CardBrowse AsRoute
					=> Wrapped;
				
				public IKokoroApi.IV2.ILimitedApi.ICardBrowse SetFilterLimited(bool? value)
				{
					FilterLimited = value;
					return this;
				}
			}
			
			internal sealed class ModifyLimitedUsesArgs : IKokoroApi.IV2.ILimitedApi.IHook.IModifyLimitedUsesArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly ModifyLimitedUsesArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
				public int BaseUses { get; internal set; }
				public int Uses { get; set; }
			}
			
			internal sealed class IsSingleUseLimitedArgs : IKokoroApi.IV2.ILimitedApi.IHook.IIsSingleUseLimitedArgs
			{
				// ReSharper disable once MemberHidesStaticFromOuterClass
				internal static readonly IsSingleUseLimitedArgs Instance = new();
				
				public State State { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
			}
		}
	}
}

internal sealed class LimitedManager : HookManager<IKokoroApi.IV2.ILimitedApi.IHook>
{
	internal const int DefaultLimitedUses = 2;
	
	internal static readonly LimitedManager Instance = new();
	
	internal static ICardTraitEntry Trait = null!;

	private static readonly Dictionary<string, Dictionary<Upgrade, int>> BaseLimitedUses = [];
	private static readonly Dictionary<int, Spr> Icons = [];
	
	private LimitedManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Limited", new()
		{
			Icon = (state, card) => ObtainIcon(card is null ? 10 : GetLimitedUses(state, card)),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["limited", "name"]).Localize,
			Tooltips = (state, card) =>
			{
				string description;
				if (card is null)
					description = ModEntry.Instance.Localizations.Localize(["limited", "description", "withoutCard"]);
				else if (state.route is Combat)
					description = ModEntry.Instance.Localizations.Localize(["limited", "description", "stateful"], new { Count = GetLimitedUses(state, card) });
				else
					description = ModEntry.Instance.Localizations.Localize(["limited", "description", "outOfCombat"], new { Count = GetStartingLimitedUses(state, card) });

				return [
					new GlossaryTooltip($"cardtrait.{MethodBase.GetCurrentMethod()!.DeclaringType!.Namespace!}::Limited")
					{
						Icon = ObtainIcon(card is null ? 10 : GetLimitedUses(DB.fakeState, card)),
						TitleColor = Colors.cardtrait,
						Title = ModEntry.Instance.Localizations.Localize(["limited", "name"]),
						Description = description,
					},
					new TTGlossary("cardtrait.exhaust"),
				];
			}
		});

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			foreach (var card in state.deck)
				ResetLimitedUses(state, card);
		});

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state) =>
		{
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;
			SetLimitedUses(state, card, GetLimitedUses(state, card) - 1);
		});

		ModEntry.Instance.Helper.Content.Cards.OnGetFinalDynamicCardTraitOverrides += OnGetFinalDynamicCardTraitOverrides;

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
	}

	[EventPriority(double.MaxValue)]
	private static void OnGetFinalDynamicCardTraitOverrides(object? sender, GetFinalDynamicCardTraitOverridesEventArgs args)
	{
		if (!args.TraitStates[Trait].IsActive)
			return;
		if (GetLimitedUses(args.State, args.Card) > 1)
			return;

		args.SetOverride(
			!args.CardData.unremovableAtShops && IsSingleUseLimited(args.State, args.Card)
				? ModEntry.Instance.Helper.Content.Cards.SingleUseCardTrait
				: ModEntry.Instance.Helper.Content.Cards.ExhaustCardTrait,
			true
		);
	}

	public static int GetBaseLimitedUses(string key, Upgrade upgrade)
	{
		if (!BaseLimitedUses.TryGetValue(key, out var perUpgrade))
			return DefaultLimitedUses;
		return perUpgrade.GetValueOrDefault(upgrade, DefaultLimitedUses);
	}

	public static int GetStartingLimitedUses(State state, Card card)
	{
		var args = ApiImplementation.V2Api.LimitedApi.ModifyLimitedUsesArgs.Instance;
		args.State = state;
		args.Card = card;
		args.BaseUses = GetBaseLimitedUses(card.Key(), card.upgrade);
		args.Uses = args.BaseUses;

		foreach (var hook in Instance.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			if (hook.ModifyLimitedUses(args))
				break;

		return Math.Max(args.Uses, 1);
	}

	public static void SetBaseLimitedUses(string key, int value)
	{
		SetBaseLimitedUses(key, Upgrade.None, value);
		SetBaseLimitedUses(key, Upgrade.A, value);
		SetBaseLimitedUses(key, Upgrade.B, value);
	}

	public static void SetBaseLimitedUses(string key, Upgrade upgrade, int value)
	{
		if (!BaseLimitedUses.TryGetValue(key, out var perUpgrade))
		{
			perUpgrade = [];
			BaseLimitedUses[key] = perUpgrade;
		}
		perUpgrade[upgrade] = value;
	}
	
	public static int GetLimitedUses(State state, Card card)
	{
		if (ModEntry.Instance.Helper.ModData.TryGetModData(card, "LimitedUses", out int value))
			return value;
		return GetStartingLimitedUses(state, card);
	}

	public static void SetLimitedUses(State state, Card card, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(card, "LimitedUses", Math.Max(value, 1));

	public static void ResetLimitedUses(State state, Card card)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(card, "LimitedUses");

	public static bool IsSingleUseLimited(State state, Card card)
	{
		var args = ApiImplementation.V2Api.LimitedApi.IsSingleUseLimitedArgs.Instance;
		args.State = state;
		args.Card = card;
		
		foreach (var hook in Instance.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			if (hook.IsSingleUseLimited(args) is { } result)
				return result;
		return false;
	}

	internal static Spr ObtainIcon(int amount)
	{
		amount = Math.Clamp(amount, 0, 10);
		if (Icons.TryGetValue(amount, out var icon))
			return icon;

		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Limited{amount}", () =>
		{
			var exhaustIcon = SpriteLoader.Get(StableSpr.icons_exhaust)!;
			return TextureUtils.CreateTexture(exhaustIcon.Width, exhaustIcon.Height, () =>
			{
				Draw.Sprite(exhaustIcon, 0, 0);

				var text = amount > 9 ? "+" : amount.ToString();
				var textRect = Draw.Text(text, 0, 0, outline: Colors.black, dontDraw: true, dontSubstituteLocFont: true);
				Draw.Text(text, exhaustIcon.Width - textRect.w, exhaustIcon.Height - textRect.h - 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
			});
		}).Sprite;

		Icons[amount] = icon;
		return icon;
	}

	private static bool Card_RenderAction_Prefix(G g, CardAction action, bool dontDraw, ref int __result)
	{
		if (action is not ChangeLimitedUsesAction usesAction)
			return true;

		var position = g.Push(rect: new()).rect.xy;
		var initialX = (int)position.x;

		if (!dontDraw)
			Draw.Sprite(ObtainIcon(10), position.x, position.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
		position.x += 10;

		if (usesAction.Mode == AStatusMode.Set)
		{
			position.x += 1;
			if (!dontDraw)
				Draw.Text("=", position.x, position.y, dontSubstituteLocFont: true);
			position.x += 6;
		}

		if (!dontDraw)
			BigNumbers.Render(usesAction.Amount, position.x, position.y, usesAction.disabled ? Colors.disabledText : Colors.textMain);
		position.x += usesAction.Amount.ToString().Length * 6;

		__result = (int)position.x - initialX;
		g.Pop();

		return false;
	}
	
	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;
		
		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "FilterLimited", ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterLimited"));
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		var filterLimited = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterLimited");
		if (filterLimited is null)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
			if (filterLimited is not null && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(g.state, __result[i], Trait) != filterLimited.Value)
				__result.RemoveAt(i);
	}
}

internal sealed class LimitedUsesVariableHint : AVariableHint, IKokoroApi.IV2.ILimitedApi.IVariableHint
{
	public required int CardId { get; set; }

	[JsonIgnore]
	public AVariableHint AsCardAction
		=> this;

	public LimitedUsesVariableHint()
	{
		this.hand = true;
	}

	public override Icon? GetIcon(State s)
		=> new() { path = LimitedManager.ObtainIcon(10) };

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip("action.xHintLimitedUses.desc")
			{
				Description = ModEntry.Instance.Localizations.Localize(["x", "LimitedUses", s.route is Combat ? "stateful" : "stateless"], new { Count = s.FindCard(CardId) is { } card ? LimitedManager.GetLimitedUses(s, card) : 0 })
			}
		];

	public IKokoroApi.IV2.ILimitedApi.IVariableHint SetCardId(int value)
	{
		CardId = value;
		return this;
	}
}

internal sealed class ChangeLimitedUsesAction : CardAction, IKokoroApi.IV2.ILimitedApi.IChangeLimitedUsesAction
{
	public required int CardId { get; set; }
	public required int Amount { get; set; }
	public AStatusMode Mode { get; set; } = AStatusMode.Add;

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override Icon? GetIcon(State s)
		=> new(LimitedManager.ObtainIcon(10), Amount, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{GetType().Namespace!}::ChangeLimitedUses::{Mode}")
			{
				Icon = LimitedManager.ObtainIcon(10),
				TitleColor = Colors.action,
				Title = Mode switch
				{
					AStatusMode.Add => ModEntry.Instance.Localizations.Localize(["limited", "changeLimitedUses", "Add", "name", Amount >= 0 ? "positive" : "negative"]),
					_ => ModEntry.Instance.Localizations.Localize(["limited", "changeLimitedUses", Mode.ToString(), "name"])
				},
				Description = Mode switch
				{
					AStatusMode.Add => ModEntry.Instance.Localizations.Localize(["limited", "changeLimitedUses", "Add", "description", Amount >= 0 ? "positive" : "negative"], new { Amount = Math.Abs(Amount) }),
					_ => ModEntry.Instance.Localizations.Localize(["limited", "changeLimitedUses", Mode.ToString(), "description"], new { Amount = Amount })
				}
			}
		];

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		if (s.FindCard(CardId) is not { } card)
		{
			timer = 0;
			return;
		}

		var currentAmount = LimitedManager.GetLimitedUses(s, card);
		var newAmount = Mode switch
		{
			AStatusMode.Add => currentAmount + Amount,
			AStatusMode.Mult => currentAmount * Amount,
			_ => Amount,
		};

		LimitedManager.SetLimitedUses(s, card, newAmount);
		if (newAmount > 0)
			return;
		
		if (!card.GetDataWithOverrides(s).unremovableAtShops && LimitedManager.IsSingleUseLimited(s, card))
		{
			s.RemoveCardFromWhereverItIs(CardId);
		}
		else if (!c.exhausted.Contains(card))
		{
			s.RemoveCardFromWhereverItIs(CardId);
			c.SendCardToExhaust(s, card);
		}
	}
	
	public IKokoroApi.IV2.ILimitedApi.IChangeLimitedUsesAction SetCardId(int value)
	{
		CardId = value;
		return this;
	}

	public IKokoroApi.IV2.ILimitedApi.IChangeLimitedUsesAction SetAmount(int value)
	{
		Amount = value;
		return this;
	}

	public IKokoroApi.IV2.ILimitedApi.IChangeLimitedUsesAction SetMode(AStatusMode value)
	{
		Mode = value;
		return this;
	}
}