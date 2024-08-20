using daisyowl.text;
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

internal static class DrawPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Draw), nameof(Draw.Text)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Draw_Text_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Draw), nameof(Draw.RenderCharacter)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Draw_RenderCharacter_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Draw), nameof(Draw.RenderCharacterOutline)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Draw_RenderCharacterOutline_Transpiler))
		);
	}

	private static bool ShouldAlsoUsePointClamp(Spr atlas)
		=> atlas == Instance.Content.PinchCompactFont.atlas || atlas == Instance.Content.PinchCompactFont.outlined?.atlas;

	private static void Draw_Text_Prefix(Font? font, bool dontSubstituteLocFont, ref double extraScale)
	{
		if (!DB.currentLocale.isHighRes)
			return;
		if (dontSubstituteLocFont)
			return;
		if (font != Instance.Content.PinchCompactFont && font != Instance.Content.PinchCompactFont.outlined)
			return;

		extraScale *= 10.0 / 48;
	}

	private static IEnumerable<CodeInstruction> Draw_RenderCharacter_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.LdcI4((int)StableSpr.fonts_pinch_atlas32),
					ILMatches.Beq.GetBranchTarget(out var branchTarget)
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ShouldAlsoUsePointClamp))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget.Value)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static IEnumerable<CodeInstruction> Draw_RenderCharacterOutline_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.LdcI4((int)StableSpr.fonts_pinch_atlas32),
					ILMatches.Beq.GetBranchTarget(out var branchTarget)
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ShouldAlsoUsePointClamp))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget.Value)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}
}
