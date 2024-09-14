using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public ACardSelect MakeCustomCardBrowse(ACardSelect action, ICustomCardBrowseSource source)
		{
			var custom = Mutil.DeepCopy(action);
			custom.browseSource = (CardBrowse.Source)999999;
			ModEntry.Instance.Api.SetExtensionData(custom, "CustomCardBrowseSource", source);
			return custom;
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
				.Find(
					ILMatches.Stloc<CardBrowse>(originalMethod)
				)
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
		if (!ModEntry.Instance.Api.TryGetExtensionData(select, "CustomCardBrowseSource", out ICustomCardBrowseSource? customCardSource))
			return;

		ModEntry.Instance.Api.SetExtensionData(browse, "CustomCardBrowseSource", customCardSource);
	}
	
	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		if (!ModEntry.Instance.Api.TryGetExtensionData<ICustomCardBrowseSource>(__instance, "CustomCardBrowseSource", out var customCardSource))
			return;

		__result = customCardSource.GetCards(g.state, g.state.route as Combat);
	}

	private static IEnumerable<CodeInstruction> CardBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("browseSource"),
					ILMatches.Stloc<CardBrowse.Source>(originalMethod),
					ILMatches.Ldloc<CardBrowse.Source>(originalMethod),
					ILMatches.Instruction(OpCodes.Switch),
					ILMatches.Br.GetBranchTarget(out var failBranchTarget)
				)
				.PointerMatcher(failBranchTarget)
				.Advance(-1)
				.GetBranchTarget(out var successBranchTarget)
				.Advance(-1)
				.CreateLdlocaInstruction(out var ldlocaTitle)
				.PointerMatcher(failBranchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocaTitle,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Transpiler_GetCardSourceText))),
					new CodeInstruction(OpCodes.Brtrue, successBranchTarget)
				)
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
		if (!ModEntry.Instance.Api.TryGetExtensionData<ICustomCardBrowseSource>(self, "CustomCardBrowseSource", out var customCardSource))
			return false;

		var cards = customCardSource.GetCards(g.state, g.state.route as Combat);
		title = customCardSource.GetTitle(g.state, g.state.route as Combat, cards);
		return true;
	}
}