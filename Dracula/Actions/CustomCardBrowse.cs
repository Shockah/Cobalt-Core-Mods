using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Dracula;

internal sealed class CustomCardBrowse
{
	internal record CustomCardSource(
		Func<State, Combat?, List<Card>, string> TitleProvider,
		Func<State, Combat?, List<Card>> CardProvider
	);

	private static readonly Dictionary<CardBrowse.Source, CustomCardSource> CustomCardSources = [];

	internal static void RegisterCustomCardSource(CardBrowse.Source key, CustomCardSource source)
		=> CustomCardSources[key] = source;

	internal static void ApplyPatches(Harmony harmony, ILogger logger)
	{
		harmony.TryPatch(
			logger: logger,
			original: () => AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler))
		);
		harmony.TryPatch(
			logger: logger,
			original: () => AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> CardBrowse_GetCardList_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence,
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("_empty"),
					ILMatches.Stloc<List<Card>>(originalMethod).CreateLdlocInstruction(out var ldlocCards).CreateStlocInstruction(out var stlocCards)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					ldlocCards,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler_ModifyCards))),
					stlocCards
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static List<Card> CardBrowse_GetCardList_Transpiler_ModifyCards(List<Card> actions, CardBrowse self, G g)
	{
		if (!CustomCardSources.TryGetValue(self.browseSource, out var customCardSource))
			return actions;
		return customCardSource.CardProvider(g.state, g.state.route as Combat);
	}

	private static IEnumerable<CodeInstruction> CardBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
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
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static bool CardBrowse_Render_Transpiler_GetCardSourceText(CardBrowse self, G g, ref string title)
	{
		if (!CustomCardSources.TryGetValue(self.browseSource, out var customCardSource))
			return false;

		var cards = customCardSource.CardProvider(g.state, g.state.route as Combat);
		title = customCardSource.TitleProvider(g.state, g.state.route as Combat, cards);
		return true;
	}
}