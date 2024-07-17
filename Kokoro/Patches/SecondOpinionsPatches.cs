using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

internal static class SecondOpinionsPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(SecondOpinions), nameof(SecondOpinions.GetActions)),
			transpiler: new HarmonyMethod(typeof(SecondOpinionsPatches), nameof(SecondOpinions_GetActions_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> SecondOpinions_GetActions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Instruction(OpCodes.Ldtoken),
					ILMatches.Call("GetTypeFromHandle"),
					ILMatches.Call("GetValues")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Pop),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(SecondOpinionsPatches), nameof(SecondOpinions_GetActions_Transpiler_ModifyDeckTypes)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static Deck[] SecondOpinions_GetActions_Transpiler_ModifyDeckTypes(State state)
		=> Instance.Api.GetCardsEverywhere(state).Select(c => c.GetMeta().deck).Distinct().ToArray();
}
