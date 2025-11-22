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
	private static bool IsDuringDroneMove;
	
	private static void HandleMove()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADroneMove), nameof(ADroneMove.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADroneMove), nameof(ADroneMove.DoMoveSingleDrone)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_DoMoveSingleDrone_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_DoMoveSingleDrone_Finalizer)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_DoMoveSingleDrone_Transpiler))
		);
	}

	private static void ADroneMove_Begin_Prefix()
		=> IsDuringDroneMove = true;

	private static void ADroneMove_Begin_Finalizer()
		=> IsDuringDroneMove = false;

	private static void ADroneMove_DoMoveSingleDrone_Prefix(Combat c, int x, out StuffBase? __state)
	{
		if (IsDuringDroneMove)
		{
			__state = null;
			return;
		}

		var toMove = PopStackedObject(c, x, true);
		var leftover = c.stuff.GetValueOrDefault(x);
		__state = leftover;

		if (toMove is null)
			c.stuff.Remove(x);
		else
			c.stuff[x] = toMove;
	}

	private static void ADroneMove_DoMoveSingleDrone_Finalizer(Combat c, int x, in StuffBase? __state)
	{
		if (IsDuringDroneMove)
			return;
		if (__state is null)
			return;
		
		var potentialNewStuff = c.stuff.GetValueOrDefault(x);
		c.stuff.Remove(x);
		
		PushStackedObject(c, x, __state);
		if (potentialNewStuff is not null)
			PushStackedObject(c, x, potentialNewStuff);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> ADroneMove_DoMoveSingleDrone_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).GetLocalIndex(out var objectLocalIndex),
					ILMatches.Call("Invincible"),
					ILMatches.Brfalse,
				])
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneMove_DoMoveSingleDrone_Transpiler_ApplyFakeWobblyIfNeeded))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void ADroneMove_DoMoveSingleDrone_Transpiler_ApplyFakeWobblyIfNeeded(StuffBase @object, Combat combat)
	{
		if (!combat.stuff.TryGetValue(@object.x, out var existingObject))
			return;
		if (!@object.Invincible())
			return;
		
		SetWobbly(existingObject);
	}
}