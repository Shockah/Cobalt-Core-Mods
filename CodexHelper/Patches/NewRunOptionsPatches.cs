﻿using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Nickel;

namespace Shockah.CodexHelper;

internal static class NewRunOptionsPatches
{
	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), "DifficultyOptions"),
			transpiler: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_DifficultyOptions_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> NewRunOptions_DifficultyOptions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldsfld("difficulties"),
					ILMatches.AnyLdloc,
					ILMatches.Call("get_Item"),
					ILMatches.Stloc<NewRunOptions.DifficultyLevel>(originalMethod).CreateLdlocInstruction(out var ldlocDifficulty)
				)
				.Find(ILMatches.Call("SelectButtonText"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Dup),
					ldlocDifficulty,
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_DifficultyOptions_Transpiler_DrawCompletionStarAndAddTooltip)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void NewRunOptions_DifficultyOptions_Transpiler_DrawCompletionStarAndAddTooltip(SharedArt.ButtonResult buttonResult, NewRunOptions.DifficultyLevel difficulty, G g, RunConfig runConfig)
	{
		var selectedCharKeys = runConfig.selectedChars.Select(d => d.Key()).ToHashSet();
		var maxBeatenDifficulty = g.state.bigStats.combos
			.Select(kvp => (Combo: BigStats.ParseComboKey(kvp.Key), Stats: kvp.Value))
			.Where(e => e.Combo is not null)
			.Select(e => (Combo: e.Combo!.Value, Stats: e.Stats))
			.Where(e =>
			{
				// this won't matter for vanilla, but if a mod comes out that makes 2-crew runs possible, this will behave correctly now
				if (runConfig.IsValid(g))
					return selectedCharKeys.SetEquals(e.Combo.decks.Select(d => d.Key()));
				else
					return !selectedCharKeys.Except(e.Combo.decks.Select(d => d.Key())).Any();
			})
			.Select(e => e.Stats.maxDifficultyWin ?? int.MinValue)
			.DefaultIfEmpty(int.MinValue)
			.Max();

		if (maxBeatenDifficulty < difficulty.level)
			return;

		var color = Colors.white.fadeAlpha(runConfig.IsValid(g) ? 1.0 : 0.5);
		Draw.Sprite(StableSpr.icons_ace, buttonResult.v.x - 10, buttonResult.v.y + 4, color: color);

		if (buttonResult.isHover)
			g.tooltips.Add(buttonResult.v + new Vec(63), selectedCharKeys.Count == 0 ? I18n.DifficultyBeatenByAnyCrew : I18n.DifficultyBeatenByThisCrew);
	}
}
