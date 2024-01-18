using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Dracula;

internal sealed class CustomCardBrowse
{
	internal record CustomCardSource(
		Func<State, Combat, List<Card>, string> TitleProvider,
		Func<State, Combat, List<Card>> CardProvider
	);

	private static readonly Dictionary<CardBrowse.Source, CustomCardSource> CustomCardSources = [];

	internal static void RegisterCustomCardSource(CardBrowse.Source key, CustomCardSource source)
		=> CustomCardSources[key] = source;

	internal static void ApplyPatches()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
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
					ILMatches.Stloc<List<Card>>(originalMethod)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler_ModifyCards)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static List<Card> CardBrowse_GetCardList_Transpiler_ModifyCards(List<Card> actions, CardBrowse self, G g)
	{
		if (!CustomCardSources.TryGetValue(self.browseSource, out var customCardSource))
			return actions;
		if (g.state.route is not Combat combat)
			return actions;
		return customCardSource.CardProvider(g.state, combat);
	}

	private static IEnumerable<CodeInstruction> CardBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Stloc<string>(originalMethod).CreateStlocInstruction(out var stlocText),
					ILMatches.Br,
					ILMatches.Ldloc<CardBrowse.Source>(originalMethod).Anchor(out var replaceAnchorStart).ExtractLabels(out var labels),
					ILMatches.Instruction(OpCodes.Box),
					ILMatches.Call("ThrowSwitchExpressionException").Anchor(out var replaceAnchorEnd)
				)
				.Anchors().PointerMatcher(replaceAnchorStart)
				.Anchors().EncompassUntil(replaceAnchorEnd)
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Transpiler_GetCardSourceText))),
					stlocText
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static string CardBrowse_Render_Transpiler_GetCardSourceText(CardBrowse self, G g)
	{
		if (!CustomCardSources.TryGetValue(self.browseSource, out var customCardSource))
			throw new InvalidOperationException();
		if (g.state.route is not Combat combat)
			throw new InvalidOperationException();

		var cards = customCardSource.CardProvider(g.state, combat);
		return customCardSource.TitleProvider(g.state, combat, cards);
	}
}
