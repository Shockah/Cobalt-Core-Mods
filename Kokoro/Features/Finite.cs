using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IFiniteApi Finite { get; } = new FiniteApi();
		
		public sealed class FiniteApi : IKokoroApi.IV2.IFiniteApi
		{
			public ICardTraitEntry Trait
				=> FiniteManager.Trait;

			public int DefaultFiniteUses
				=> FiniteManager.DefaultFiniteUses;
			
			public int GetBaseFiniteUses(string key, Upgrade upgrade)
				=> FiniteManager.GetBaseFiniteUses(key, upgrade);

			public void SetBaseFiniteUses(string key, int value)
				=> FiniteManager.SetBaseFiniteUses(key, value);

			public void SetBaseFiniteUses(string key, Upgrade upgrade, int value)
				=> FiniteManager.SetBaseFiniteUses(key, upgrade, value);

			public int GetStartingFiniteUses(State state, Card card)
				=> FiniteManager.GetStartingFiniteUses(state, card);

			public int GetFiniteUses(State state, Card card)
				=> FiniteManager.GetFiniteUses(state, card);

			public void SetFiniteUses(State state, Card card, int value)
				=> FiniteManager.SetFiniteUses(state, card, value);

			public void ResetFiniteUses(State state, Card card)
				=> FiniteManager.ResetFiniteUses(state, card);

			public IKokoroApi.IV2.IFiniteApi.IVariableHint? AsVariableHint(AVariableHint action)
				=> action as IKokoroApi.IV2.IFiniteApi.IVariableHint;

			public IKokoroApi.IV2.IFiniteApi.IVariableHint MakeVariableHint(int cardId)
				=> new FiniteUsesVariableHint { CardId = cardId };

			public IKokoroApi.IV2.IFiniteApi.IChangeFiniteUsesAction? AsChangeFiniteUsesAction(CardAction action)
				=> action as IKokoroApi.IV2.IFiniteApi.IChangeFiniteUsesAction;

			public IKokoroApi.IV2.IFiniteApi.IChangeFiniteUsesAction MakeChangeFiniteUsesAction(int cardId, int amount, AStatusMode mode = AStatusMode.Add)
				=> new ChangeFiniteUsesAction { CardId = cardId, Amount = amount, Mode = mode };

			public IKokoroApi.IV2.IFiniteApi.ICardSelect ModifyCardSelect(ACardSelect action)
				=> new CardSelectWrapper { Wrapped = action };

			public IKokoroApi.IV2.IFiniteApi.ICardBrowse ModifyCardBrowse(CardBrowse route)
				=> new CardBrowseWrapper { Wrapped = route };

			public Spr GetIcon(int amount)
				=> FiniteManager.ObtainIcon(amount);

			public void RegisterHook(IKokoroApi.IV2.IFiniteApi.IHook hook, double priority = 0)
				=> FiniteManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IFiniteApi.IHook hook)
				=> FiniteManager.Instance.Unregister(hook);
			
			private sealed class CardSelectWrapper : IKokoroApi.IV2.IFiniteApi.ICardSelect
			{
				public required ACardSelect Wrapped { get; init; }

				public bool? FilterFinite
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterFinite");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterFinite", value);
				}

				[JsonIgnore]
				public ACardSelect AsCardAction
					=> Wrapped;
				
				public IKokoroApi.IV2.IFiniteApi.ICardSelect SetFilterFinite(bool? value)
				{
					FilterFinite = value;
					return this;
				}
			}
			
			private sealed class CardBrowseWrapper : IKokoroApi.IV2.IFiniteApi.ICardBrowse
			{
				public required CardBrowse Wrapped { get; init; }

				public bool? FilterFinite
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterFinite");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterFinite", value);
				}

				public CardBrowse AsRoute
					=> Wrapped;
				
				public IKokoroApi.IV2.IFiniteApi.ICardBrowse SetFilterFinite(bool? value)
				{
					FilterFinite = value;
					return this;
				}
			}
			
			internal sealed class ModifyFiniteUsesArgs : IKokoroApi.IV2.IFiniteApi.IHook.IModifyFiniteUsesArgs
			{
				public State State { get; internal set; } = null!;
				public Card Card { get; internal set; } = null!;
				public int BaseUses { get; internal set; }
				public int Uses { get; set; }
			}
		}
	}
}

internal sealed class FiniteManager : HookManager<IKokoroApi.IV2.IFiniteApi.IHook>
{
	internal const int DefaultFiniteUses = 3;
	
	internal static readonly FiniteManager Instance = new();
	
	internal static ICardTraitEntry Trait = null!;

	private static readonly Dictionary<string, Dictionary<Upgrade, int>> BaseFiniteUses = [];
	private static readonly Dictionary<int, Spr> Icons = [];
	
	private FiniteManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Finite", new()
		{
			Icon = (state, card) => ObtainIcon(card is null ? 10 : GetFiniteUses(state, card)),
			Name = ModEntry.Instance.AnyLocalizations.Bind(["finite", "name"]).Localize,
			Tooltips = (state, card) =>
			{
				string description;
				if (card is null)
					description = ModEntry.Instance.Localizations.Localize(["finite", "description", "withoutCard"]);
				else if (state.route is Combat)
					description = ModEntry.Instance.Localizations.Localize(["finite", "description", "stateful"], new { Count = GetFiniteUses(state, card) });
				else
					description = ModEntry.Instance.Localizations.Localize(["finite", "description", "outOfCombat"], new { Count = GetStartingFiniteUses(state, card) });

				return [
					new GlossaryTooltip($"cardtrait.{MethodBase.GetCurrentMethod()!.DeclaringType!.Namespace!}::Finite")
					{
						Icon = ObtainIcon(card is null ? 10 : GetFiniteUses(DB.fakeState, card)),
						TitleColor = Colors.cardtrait,
						Title = ModEntry.Instance.Localizations.Localize(["finite", "name"]),
						Description = description,
					},
				];
			}
		});
		
		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnEnd), (State state, Combat combat) =>
		{
			foreach (var card in state.deck)
				ResetFiniteUses(state, card);
			foreach (var card in combat.discard)
				ResetFiniteUses(state, card);
			foreach (var card in combat.exhausted)
				ResetFiniteUses(state, card);
			foreach (var card in combat.hand)
				ResetFiniteUses(state, card);
		});

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			foreach (var card in state.deck)
				ResetFiniteUses(state, card);
		});

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnPlayerPlayCard), (Card card, State state) =>
		{
			if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				return;
			SetFiniteUses(state, card, GetFiniteUses(state, card) - 1);
		});

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler))
		);
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

	public static int GetBaseFiniteUses(string key, Upgrade upgrade)
	{
		if (!BaseFiniteUses.TryGetValue(key, out var perUpgrade))
			return DefaultFiniteUses;
		return perUpgrade.GetValueOrDefault(upgrade, DefaultFiniteUses);
	}

	public static int GetStartingFiniteUses(State state, Card card)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.FiniteApi.ModifyFiniteUsesArgs>();
		try
		{
			args.State = state;
			args.Card = card;
			args.BaseUses = GetBaseFiniteUses(card.Key(), card.upgrade);
			args.Uses = args.BaseUses;

			foreach (var hook in Instance.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				if (hook.ModifyFiniteUses(args))
					break;

			return Math.Max(args.Uses, 1);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public static void SetBaseFiniteUses(string key, int value)
	{
		SetBaseFiniteUses(key, Upgrade.None, value);
		SetBaseFiniteUses(key, Upgrade.A, value);
		SetBaseFiniteUses(key, Upgrade.B, value);
	}

	public static void SetBaseFiniteUses(string key, Upgrade upgrade, int value)
	{
		ref var perUpgrade = ref CollectionsMarshal.GetValueRefOrAddDefault(BaseFiniteUses, key, out var perUpgradeExists);
		if (!perUpgradeExists)
			perUpgrade = [];
		perUpgrade![upgrade] = value;
	}
	
	public static int GetFiniteUses(State state, Card card)
	{
		if (ModEntry.Instance.Helper.ModData.TryGetModData(card, "FiniteUses", out int value))
			return value;
		return GetStartingFiniteUses(state, card);
	}

	public static void SetFiniteUses(State state, Card card, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(card, "FiniteUses", Math.Max(value, 1));

	public static void ResetFiniteUses(State state, Card card)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(card, "FiniteUses");

	internal static Spr ObtainIcon(int amount)
	{
		amount = Math.Clamp(amount, 0, 10);
		if (Icons.TryGetValue(amount, out var icon))
			return icon;

		icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite($"Finite{amount}", () =>
		{
			var infiniteIcon = SpriteLoader.Get(StableSpr.icons_infinite)!;
			return TextureUtils.CreateTexture(new(infiniteIcon.Width, infiniteIcon.Height)
			{
				Actions = _ =>
				{
					Draw.Sprite(infiniteIcon, 0, 0);

					var text = amount > 9 ? "+" : amount.ToString();
					var textRect = Draw.Text(text, 0, 0, outline: Colors.black, dontDraw: true, dontSubstituteLocFont: true);
					Draw.Text(text, infiniteIcon.Width - textRect.w, infiniteIcon.Height - textRect.h - 1, color: Colors.white, outline: Colors.black, dontSubstituteLocFont: true);
				},
			});
		}).Sprite;

		Icons[amount] = icon;
		return icon;
	}

	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<CardData>(originalMethod),
					ILMatches.Ldfld(nameof(CardData.infinite)),
					ILMatches.Brfalse,
					ILMatches.Ldloc<bool>(originalMethod).GetLocalIndex(out var actuallyExhaustLocalIndex),
					ILMatches.LdcI4(0),
					ILMatches.Instruction(OpCodes.Ceq),
					ILMatches.Br,
					ILMatches.LdcI4(0),
					ILMatches.Stloc<bool>(originalMethod).GetLocalIndex(out var actuallyInfiniteLocalIndex),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldloca, actuallyInfiniteLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, actuallyExhaustLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_ModifyActuallyExhaust))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void Combat_TryPlayCard_Transpiler_ModifyActuallyExhaust(State state, Card card, ref bool actuallyInfinite, bool actuallyExhaust)
	{
		if (actuallyInfinite)
			return;
		if (actuallyExhaust)
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Trait))
			return;
		if (GetFiniteUses(state, card) <= 1)
			return;
		actuallyInfinite = true;
	}

	private static bool Card_RenderAction_Prefix(G g, CardAction action, bool dontDraw, ref int __result)
	{
		if (action is not ChangeFiniteUsesAction usesAction)
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
		
		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "FilterFinite", ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterFinite"));
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		var filterFinite = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterFinite");
		if (filterFinite is null)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
			if (filterFinite is not null && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(g.state, __result[i], Trait) != filterFinite.Value)
				__result.RemoveAt(i);
	}
}

internal sealed class FiniteUsesVariableHint : AVariableHint, IKokoroApi.IV2.IFiniteApi.IVariableHint
{
	public required int CardId { get; set; }

	[JsonIgnore]
	public AVariableHint AsCardAction
		=> this;

	public FiniteUsesVariableHint()
	{
		this.hand = true;
	}

	public override Icon? GetIcon(State s)
		=> new() { path = FiniteManager.ObtainIcon(10) };

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip("action.xHintFiniteUses.desc")
			{
				Description = ModEntry.Instance.Localizations.Localize(["x", "FiniteUses", s.route is Combat ? "stateful" : "stateless"], new { Count = s.FindCard(CardId) is { } card ? FiniteManager.GetFiniteUses(s, card) : 0 })
			}
		];

	public IKokoroApi.IV2.IFiniteApi.IVariableHint SetCardId(int value)
	{
		CardId = value;
		return this;
	}
}

internal sealed class ChangeFiniteUsesAction : CardAction, IKokoroApi.IV2.IFiniteApi.IChangeFiniteUsesAction
{
	public required int CardId { get; set; }
	public required int Amount { get; set; }
	public AStatusMode Mode { get; set; } = AStatusMode.Add;

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override Icon? GetIcon(State s)
		=> new(FiniteManager.ObtainIcon(10), Amount, Colors.textMain);

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new GlossaryTooltip($"action.{GetType().Namespace!}::ChangeFiniteUses::{Mode}")
			{
				Icon = FiniteManager.ObtainIcon(10),
				TitleColor = Colors.action,
				Title = Mode switch
				{
					AStatusMode.Add => ModEntry.Instance.Localizations.Localize(["finite", "changeFiniteUses", "Add", "name", Amount >= 0 ? "positive" : "negative"]),
					_ => ModEntry.Instance.Localizations.Localize(["finite", "changeFiniteUses", Mode.ToString(), "name"])
				},
				Description = Mode switch
				{
					AStatusMode.Add => ModEntry.Instance.Localizations.Localize(["finite", "changeFiniteUses", "Add", "description", Amount >= 0 ? "positive" : "negative"], new { Amount = Math.Abs(Amount) }),
					_ => ModEntry.Instance.Localizations.Localize(["finite", "changeFiniteUses", Mode.ToString(), "description"], new { Amount = Amount })
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

		var currentAmount = FiniteManager.GetFiniteUses(s, card);
		var newAmount = Mode switch
		{
			AStatusMode.Add => currentAmount + Amount,
			AStatusMode.Mult => currentAmount * Amount,
			_ => Amount,
		};

		FiniteManager.SetFiniteUses(s, card, newAmount);
		if (newAmount > 0)
			return;

		if (c.hand.Contains(card))
		{
			s.RemoveCardFromWhereverItIs(CardId);
			c.SendCardToDiscard(s, card);
		}
	}
	
	public IKokoroApi.IV2.IFiniteApi.IChangeFiniteUsesAction SetCardId(int value)
	{
		CardId = value;
		return this;
	}

	public IKokoroApi.IV2.IFiniteApi.IChangeFiniteUsesAction SetAmount(int value)
	{
		Amount = value;
		return this;
	}

	public IKokoroApi.IV2.IFiniteApi.IChangeFiniteUsesAction SetMode(AStatusMode value)
	{
		Mode = value;
		return this;
	}
}