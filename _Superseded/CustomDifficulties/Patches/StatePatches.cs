using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Shared;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace Shockah.CustomDifficulties;

internal static class StatePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(State).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PopulateRun>") && m.ReturnType == typeof(Route)),
			transpiler: new HarmonyMethod(typeof(StatePatches), nameof(State_PopulateRun_Delegate_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> State_PopulateRun_Delegate_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Instruction(OpCodes.Ldfld),
					ILMatches.LdcI4(1),
					ILMatches.Blt
				)

				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.ExtractBranchTarget(out var branchTarget)

				.Encompass(SequenceMatcherEncompassDirection.Before, 1)
				.Replace(new CodeInstruction(OpCodes.Brfalse, branchTarget))

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}
}
