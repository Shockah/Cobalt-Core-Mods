using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace Shockah.Gary;

internal partial class Stack
{
	private static void HandleMissiles()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMissileHit), nameof(AMissileHit.Update)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler))
		);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> AMissileHit_Update_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLabel(il, out var label)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler_ShouldContinue))),
					new CodeInstruction(OpCodes.Brtrue, label),
					new CodeInstruction(OpCodes.Ret),
				])
				.Find(ILMatches.Stloc<Missile>(originalMethod).GetLocalIndex(out var missileLocalIndex))
				.Find([
					ILMatches.Ldarg(3),
					ILMatches.Ldfld(nameof(Combat.stuff)),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld(nameof(AMissileHit.worldX)),
					ILMatches.Call("Remove"),
					ILMatches.Instruction(OpCodes.Pop),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldloc, missileLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler_PutStackBack))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static bool AMissileHit_Update_Transpiler_ShouldContinue(AMissileHit action, G g, Combat combat)
	{
		if (!combat.stuff.TryGetValue(action.worldX, out var @object))
			return true;

		if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(action, "ChecksForStackedObjectId", out var checksForStackedObjectId))
		{
			checksForStackedObjectId = ObtainStackedObjectId(@object);
			ModEntry.Instance.Helper.ModData.SetModData(action, "ChecksForStackedObjectId", checksForStackedObjectId);
		}

		if (checksForStackedObjectId == ObtainStackedObjectId(@object))
			return true;

		action.timer -= g.dt;
		return false;
	}

	private static void AMissileHit_Update_Transpiler_PutStackBack(AMissileHit action, Combat combat, Missile missile)
	{
		combat.stuff[action.worldX] = missile;
		RemoveStackedObject(combat, action.worldX, missile);
	}
}