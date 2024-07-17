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

internal static class BigStatsPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(BigStats), nameof(BigStats.ParseComboKey)),
			transpiler: new HarmonyMethod(typeof(BigStatsPatches), nameof(BigStats_ParseComboKey_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> BigStats_ParseComboKey_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Instruction(OpCodes.Ldelema),
					ILMatches.Call("TryParse")
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Replace(
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BigStatsPatches), nameof(BigStats_ParseComboKey_Transpiler_ParseKey)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool BigStats_ParseComboKey_Transpiler_ParseKey(string? key, out Deck result)
	{
		foreach (var deck in NewRunOptions.allChars)
		{
			if (deck.Key() == key)
			{
				result = deck;
				return true;
			}
		}
		result = default;
		return false;
	}
}
