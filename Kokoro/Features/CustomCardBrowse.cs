using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	partial class ActionApiImplementation
	{
		private sealed class V1ToV2CustomCardBrowseSourceWrapper(ICustomCardBrowseSource v1) : IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource
		{
			public IEnumerable<Tooltip> GetSearchTooltips(State state)
				=> v1.GetSearchTooltips(state);

			public string GetTitle(State state, Combat? combat, List<Card> cards)
				=> v1.GetTitle(state, combat, cards);

			public List<Card> GetCards(State state, Combat? combat)
				=> v1.GetCards(state, combat);
		}
		
		public ACardSelect MakeCustomCardBrowse(ACardSelect action, ICustomCardBrowseSource source)
		{
			var custom = Mutil.DeepCopy(action);
			custom.browseSource = (CardBrowse.Source)999999;
			ModEntry.Instance.Helper.ModData.SetOptionalModData(custom, "CustomCardBrowseSource", new V1ToV2CustomCardBrowseSourceWrapper(source));
			return custom;
		}

		public CardBrowse MakeCustomCardBrowse(CardBrowse route, ICustomCardBrowseSource source)
		{
			var custom = Mutil.DeepCopy(route);
			custom.browseSource = (CardBrowse.Source)999999;
			ModEntry.Instance.Helper.ModData.SetOptionalModData(custom, "CustomCardBrowseSource", new V1ToV2CustomCardBrowseSourceWrapper(source));
			return custom;
		}
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.ICustomCardBrowseApi CustomCardBrowse { get; } = new CustomCardBrowseApi();
		
		public sealed class CustomCardBrowseApi : IKokoroApi.IV2.ICustomCardBrowseApi
		{
			public IKokoroApi.IV2.ICustomCardBrowseApi.IAction MakeCustom(ACardSelect action)
				=> new ActionWrapper { Wrapped = Mutil.DeepCopy(action) };

			public IKokoroApi.IV2.ICustomCardBrowseApi.IRoute MakeCustom(CardBrowse route)
				=> new RouteWrapper { Wrapped = Mutil.DeepCopy(route) };
			
			private sealed class ActionWrapper : IKokoroApi.IV2.ICustomCardBrowseApi.IAction
			{
				public required ACardSelect Wrapped { get; init; }

				public IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource? CustomBrowseSource
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource>(Wrapped, "CustomCardBrowseSource");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "CustomCardBrowseSource", value);
				}

				[JsonIgnore]
				public ACardSelect AsCardAction
					=> Wrapped;
				
				public IKokoroApi.IV2.ICustomCardBrowseApi.IAction SetCustomBrowseSource(IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource? source)
				{
					CustomBrowseSource = source;
					return this;
				}
			}
			
			private sealed class RouteWrapper : IKokoroApi.IV2.ICustomCardBrowseApi.IRoute
			{
				public required CardBrowse Wrapped { get; init; }

				public IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource? CustomBrowseSource
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource>(Wrapped, "CustomCardBrowseSource");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "CustomCardBrowseSource", value);
				}

				public CardBrowse AsRoute
					=> Wrapped;
				
				public IKokoroApi.IV2.ICustomCardBrowseApi.IRoute SetCustomBrowseSource(IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource? source)
				{
					CustomBrowseSource = source;
					return this;
				}
			}
		}
	}
}

internal sealed class CustomCardBrowseManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.Method(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.Method(typeof(ACardSelect), nameof(ACardSelect.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.Method(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Transpiler))
		);
	}
	
	private static IEnumerable<CodeInstruction> ACardSelect_BeginWithRoute_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Stloc<CardBrowse>(originalMethod))
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.JustInsertion,
				[
					new CodeInstruction(OpCodes.Dup),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Transpiler_ApplySource)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void ACardSelect_BeginWithRoute_Transpiler_ApplySource(CardBrowse browse, ACardSelect select)
	{
		if (!ModEntry.Instance.Helper.ModData.TryGetModData(select, "CustomCardBrowseSource", out IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource? customCardSource))
			return;

		ModEntry.Instance.Helper.ModData.SetModData(browse, "CustomCardBrowseSource", customCardSource);
	}

	private static void ACardSelect_GetTooltips_Postfix(ACardSelect __instance, State s, ref List<Tooltip> __result)
	{
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource>(__instance, "CustomCardBrowseSource", out var customCardSource))
			return;

		__result = customCardSource.GetSearchTooltips(s).ToList();
	}
	
	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource>(__instance, "CustomCardBrowseSource", out var customCardSource))
			return;

		__result = customCardSource.GetCards(g.state, g.state.route as Combat);
	}

	private static IEnumerable<CodeInstruction> CardBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("browseSource"),
					ILMatches.Stloc<CardBrowse.Source>(originalMethod),
					ILMatches.Ldloc<CardBrowse.Source>(originalMethod),
					ILMatches.Instruction(OpCodes.Switch),
					ILMatches.Br.GetBranchTarget(out var failBranchTarget)
				])
				.PointerMatcher(failBranchTarget)
				.Advance(-1)
				.GetBranchTarget(out var successBranchTarget)
				.Advance(-1)
				.CreateLdlocaInstruction(out var ldlocaTitle)
				.PointerMatcher(failBranchTarget)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocaTitle,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Transpiler_GetCardSourceText))),
					new CodeInstruction(OpCodes.Brtrue, successBranchTarget)
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool CardBrowse_Render_Transpiler_GetCardSourceText(CardBrowse self, G g, ref string title)
	{
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<IKokoroApi.IV2.ICustomCardBrowseApi.ICustomCardBrowseSource>(self, "CustomCardBrowseSource", out var customCardSource))
			return false;

		var cards = customCardSource.GetCards(g.state, g.state.route as Combat);
		title = customCardSource.GetTitle(g.state, g.state.route as Combat, cards);
		return true;
	}
}