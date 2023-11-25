using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.CustomDifficulties;

internal static class NewRunOptionsPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly Lazy<Func<Rect>> DifficultyPosGetter = new(() => AccessTools.DeclaredField(typeof(NewRunOptions), "difficultyPos").EmitStaticGetter<Rect>());
	private static readonly Lazy<Action<Rect>> DifficultyPosSetter = new(() => AccessTools.DeclaredField(typeof(NewRunOptions), "difficultyPos").EmitStaticSetter<Rect>());

	public static void Apply(Harmony harmony)
	{
		NewRunOptions.difficulties.Insert(0, new NewRunOptions.DifficultyLevel
		{
			uiKey = "difficulty_easy",
			locKey = "newRunOptions.difficultyEasy",
			color = NewRunOptions.GetDifficultyColor(ModEntry.EasyDifficultyLevel),
			level = ModEntry.EasyDifficultyLevel
		});

		var difficultyPos = DifficultyPosGetter.Value();
		difficultyPos.y -= 10;
		DifficultyPosSetter.Value(difficultyPos);

		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(NewRunOptions), "EnsureRunConfigIsGood"),
			transpiler: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_EnsureRunConfigIsGood_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.OnMouseDown)),
			transpiler: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_OnMouseDown_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.GetDifficultyColor)),
			postfix: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_GetDifficultyColor_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.GetDifficultyColorLogbook)),
			postfix: new HarmonyMethod(typeof(NewRunOptionsPatches), nameof(NewRunOptions_GetDifficultyColorLogbook_Postfix))
		);
	}

	private static IEnumerable<CodeInstruction> NewRunOptions_EnsureRunConfigIsGood_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("difficulty"),
					ILMatches.LdcI4(1),
					ILMatches.LdcI4(3),
					ILMatches.Call("Clamp"),
					ILMatches.Stfld("difficulty")
				)
				.Remove()
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static IEnumerable<CodeInstruction> NewRunOptions_OnMouseDown_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AsGuidAnchorable()
				.Find(
					ILMatches.Ldloc<RunConfig>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldloc<NewRunOptions.DifficultyLevel>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Ldfld("level"),
					ILMatches.LdcI4(0),
					ILMatches.Instruction(OpCodes.Cgt).WithAutoAnchor(out Guid comparisonAnchor),
					ILMatches.Stfld("hardmode")
				)
				.PointerMatcher(comparisonAnchor)
				.Element(out var comparisonInstruction)
				.Replace(new CodeInstruction(OpCodes.Cgt_Un, comparisonInstruction.operand))
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void NewRunOptions_GetDifficultyColor_Postfix(int level, ref Color __result)
	{
		if (level == ModEntry.EasyDifficultyLevel)
			__result = Color.Lerp(Colors.textMain, Colors.midrow, Math.Abs(level) / 3.0);
	}

	private static void NewRunOptions_GetDifficultyColorLogbook_Postfix(int level, ref Color __result)
	{
		if (level == ModEntry.EasyDifficultyLevel)
			__result = Color.Lerp(new Color(0.2, 0.3, 0.9), Colors.midrow, Math.Pow(Math.Abs(level) / 3.0, 2.0));
	}
}
