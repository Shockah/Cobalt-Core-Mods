using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.CodexHelper;

internal static class NewRunOptionsPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(NewRunOptions), "DifficultyOptions"),
			transpiler: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_DifficultyOptions_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> NewRunOptions_DifficultyOptions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldsfld("difficulties"),
					ILMatches.AnyLdloc,
					ILMatches.Call("get_Item"),
					ILMatches.Stloc<NewRunOptions.DifficultyLevel>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.CreateLdlocInstruction(out var ldlocDifficulty)

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
			Instance.Logger!.LogError("Could not patch methods - {Mod} probably won't work.\nReason: {Exception}", Instance.Name, ex);
			return instructions;
		}
	}

	private static void NewRunOptions_DifficultyOptions_Transpiler_DrawCompletionStarAndAddTooltip(SharedArt.ButtonResult buttonResult, NewRunOptions.DifficultyLevel difficulty, G g, RunConfig runConfig)
	{
		var selectedCharKeys = runConfig.selectedChars.Select(d => d.Key()).ToHashSet();
		var maxBeatenDifficulty = g.state.bigStats.combos
			.Select(kvp => (Combo: BigStats.ParseComboKey(kvp.Key), Stats: kvp.Value))
			.Where(e => e.Combo is not null)
			.Select(e => (Combo: e.Combo!.Value, Stats: e.Stats))
			.Where(e => !selectedCharKeys.Except(e.Combo.decks.Select(d => d.Key())).Any())
			.Select(e => e.Stats.maxDifficultyWin ?? -1)
			.DefaultIfEmpty(-1)
			.Max();

		if (maxBeatenDifficulty < difficulty.level)
			return;

		var color = Colors.white.fadeAlpha(runConfig.IsValid(g) ? 1.0 : 0.5);
		Draw.Sprite(Spr.icons_ace, buttonResult.v.x - 10, buttonResult.v.y + 4, color: color);

		if (buttonResult.isHover)
			g.tooltips.Add(buttonResult.v + new Vec(63), selectedCharKeys.Count == 0 ? I18n.DifficultyBeatenByAnyCrew : I18n.DifficultyBeatenByThisCrew);
	}
}
