using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IHeavyApi Heavy { get; } = new HeavyApi();
		
		public sealed class HeavyApi : IKokoroApi.IV2.IHeavyApi
		{
			public ICardTraitEntry Trait
				=> HeavyManager.Trait;

			public bool IsHeavyUsed(State state, Card card)
				=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(card, "HeavyUsed");

			public void SetHeavyUsed(State state, Card card, bool value)
				=> ModEntry.Instance.Helper.ModData.SetModData(card, "HeavyUsed", value);

			public IKokoroApi.IV2.IHeavyApi.ICardSelect ModifyCardSelect(ACardSelect action)
				=> new CardSelectWrapper { Wrapped = action };

			public IKokoroApi.IV2.IHeavyApi.ICardBrowse ModifyCardBrowse(CardBrowse route)
				=> new CardBrowseWrapper { Wrapped = route };
			
			private sealed class CardSelectWrapper : IKokoroApi.IV2.IHeavyApi.ICardSelect
			{
				public required ACardSelect Wrapped { get; init; }

				[JsonIgnore]
				public bool? FilterHeavy
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterHeavy");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterHeavy", value);
				}

				[JsonIgnore]
				public ACardSelect AsCardAction
					=> Wrapped;
				
				public IKokoroApi.IV2.IHeavyApi.ICardSelect SetFilterHeavy(bool? value)
				{
					FilterHeavy = value;
					return this;
				}
			}
			
			private sealed class CardBrowseWrapper : IKokoroApi.IV2.IHeavyApi.ICardBrowse
			{
				public required CardBrowse Wrapped { get; init; }

				[JsonIgnore]
				public bool? FilterHeavy
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "FilterHeavy");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "FilterHeavy", value);
				}

				[JsonIgnore]
				public CardBrowse AsRoute
					=> Wrapped;
				
				public IKokoroApi.IV2.IHeavyApi.ICardBrowse SetFilterHeavy(bool? value)
				{
					FilterHeavy = value;
					return this;
				}
			}
		}
	}
}

internal sealed class HeavyManager : IKokoroApi.IV2.IFleetingApi.IHook
{
	internal static readonly HeavyManager Instance = new();
	
	internal static ICardTraitEntry Trait = null!;

	private static bool IgnoreHeavyForDiscard;
	
	private HeavyManager()
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		var traitIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Heavy.png"));
		var traitUsedIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/HeavyUsed.png"));
		Trait = ModEntry.Instance.Helper.Content.Cards.RegisterTrait("Heavy", new()
		{
			Icon = (state, card) => card is null || !ModEntry.Instance.Api.V2.Heavy.IsHeavyUsed(state, card) ? traitIcon.Sprite : traitUsedIcon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Heavy", "name", "active"]).Localize,
			Tooltips = (state, card) =>
			{
				var isUsed = card is not null && ModEntry.Instance.Api.V2.Heavy.IsHeavyUsed(state, card);
				return
				[
					new GlossaryTooltip($"cardtrait.{ModEntry.Instance.Package.Manifest.UniqueName}::Heavy")
					{
						Icon = isUsed ? traitUsedIcon.Sprite : traitIcon.Sprite,
						TitleColor = Colors.cardtrait,
						Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Heavy", "name", isUsed ? "used" : "active"]),
						Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Heavy", "description"]),
					}
				];
			}
		});
		
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToHand)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToHand_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AEndTurn), nameof(AEndTurn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AEndTurn_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AEndTurn_Begin_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DiscardHand)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DiscardHand_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADiscard), nameof(ADiscard.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADiscard_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADiscard_Begin_Finalizer))
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
	
	private static void Combat_SendCardToHand_Postfix(Combat __instance, State s, Card card)
	{
		if (!__instance.hand.Contains(card))
			return;
		if (!ModEntry.Instance.Api.V2.Heavy.IsHeavyUsed(s, card))
			return;
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, card, Trait))
			return;
		
		ModEntry.Instance.Api.V2.Heavy.SetHeavyUsed(s, card, false);
	}

	private static void AEndTurn_Begin_Prefix(Combat c, out List<ADiscard> __state)
		=> __state = c.cardActions.OfType<ADiscard>().ToList();

	private static void AEndTurn_Begin_Postfix(Combat c, in List<ADiscard> __state)
	{
		var preexistingDiscardActions = __state;
		if (c.cardActions.OfType<ADiscard>().FirstOrDefault(action => !preexistingDiscardActions.Contains(action)) is not { } discardAction)
			return;
		ModEntry.Instance.Helper.ModData.SetModData(discardAction, "IgnoreHeavy", true);
	}

	private static void ADiscard_Begin_Prefix(ADiscard __instance)
	{
		if (!ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "IgnoreHeavy"))
			return;
		IgnoreHeavyForDiscard = true;
	}

	private static void ADiscard_Begin_Finalizer()
		=> IgnoreHeavyForDiscard = false;
	
	private static IEnumerable<CodeInstruction> Combat_DiscardHand_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Call("ToList"),
					ILMatches.Stloc<List<Card>>(originalMethod).CreateLdlocaInstruction(out var ldlocaDiscardable),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocaDiscardable,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(Combat_DiscardHand_Transpiler_RemoveHeavy))),
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

	private static void Combat_DiscardHand_Transpiler_RemoveHeavy(State state, ref List<Card> cards)
	{
		if (!IgnoreHeavyForDiscard)
			return;

		for (var i = cards.Count - 1; i >= 0; i--)
		{
			var card = cards[i];
			if (ModEntry.Instance.Api.V2.Heavy.IsHeavyUsed(state, card) || !ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, Trait))
				continue;
			ModEntry.Instance.Api.V2.Heavy.SetHeavyUsed(state, card, true);
			cards.RemoveAt(i);
		}
	}
	
	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;
		
		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "FilterHeavy", ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterHeavy"));
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		var filterHeavy = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterHeavy");
		if (filterHeavy is null)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
			if (filterHeavy is not null && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(g.state, __result[i], Trait) != filterHeavy.Value)
				__result.RemoveAt(i);
	}

	public bool? ShouldExhaustViaFleeting(IKokoroApi.IV2.IFleetingApi.IHook.IShouldExhaustViaFleetingArgs args)
		=> !ModEntry.Instance.Api.V2.Heavy.IsHeavyUsed(args.State, args.Card) && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(args.State, args.Card, Trait) ? false : null;
}