using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;

namespace Shockah.Gary;

internal partial class Stack
{
	private static readonly List<(StuffBase RealObject, StuffBase? StackedObject, int WorldX)?> ForceStackedObjectStack = [];
	
	private static void HandleLifecycle()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.Update)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StuffBase_Update_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.ResetHilights)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_ResetHilights_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.BeginCardAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_BeginCardAction_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_BeginCardAction_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler))
		);
	}

	private static void PushForceStackedObject(Combat combat, CardAction action)
	{
		(StuffBase RealObject, StuffBase? StackedObject, int WorldX)? toPush = null;
		try
		{
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(action, "ForceStackedObjectId", out var forceStackedObjectId))
				return;
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<int>(action, "ForceStackedObjectWorldX", out var forceStackedObjectWorldX))
				return;
			if (!combat.stuff.TryGetValue(forceStackedObjectWorldX, out var @object))
				return;

			if (ObtainStackedObjectId(@object) == forceStackedObjectId)
			{
				toPush = (@object, null, forceStackedObjectWorldX);
				return;
			}
			
			if (GetStackedObjects(@object) is not { } stackedObjects)
				return;
			if (stackedObjects.FirstOrDefault(stackedObject => ObtainStackedObjectId(stackedObject) == forceStackedObjectId) is not { } stackedObject)
				return;
			
			toPush = (@object, stackedObject, forceStackedObjectWorldX);
			combat.stuff[forceStackedObjectWorldX] = stackedObject;
		}
		finally
		{
			ForceStackedObjectStack.Add(toPush);
		}
	}

	private static void PopForceStackedObject(Combat combat)
	{
		if (ForceStackedObjectStack.Count == 0)
			return;

		var nullableEntry = ForceStackedObjectStack[^1];
		ForceStackedObjectStack.RemoveAt(ForceStackedObjectStack.Count - 1);

		if (nullableEntry?.RealObject is not { } realObject)
			return;
		var stackedObject = nullableEntry.Value.StackedObject;
		var worldX = nullableEntry.Value.WorldX;
		
		var existingObject = combat.stuff.GetValueOrDefault(worldX);
		combat.stuff[worldX] = realObject;
		if (existingObject != stackedObject)
		{
			if (stackedObject is not null)
				RemoveStackedObject(combat, worldX, stackedObject);
			if (existingObject is not null)
				PushStackedObject(combat, worldX, existingObject);
		}
	}

	private static void StuffBase_Update_Postfix(StuffBase __instance, G g)
	{
		if (GetStackedObjects(__instance) is { } stackedObjects)
			foreach (var stackedObject in stackedObjects)
				stackedObject.Update(g);
	}

	private static void Combat_ResetHilights_Postfix(Combat __instance)
	{
		ApplyToAllStackedObjects(__instance, @object =>
		{
			if (@object.hilight > 0)
				@object.hilight--;
		});
	}

	private static void Combat_BeginCardAction_Prefix(Combat __instance, CardAction a)
		=> PushForceStackedObject(__instance, a);

	private static void Combat_BeginCardAction_Finalizer(Combat __instance)
		=> PopForceStackedObject(__instance);
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_DrainCardActions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("currentCardAction"),
					ILMatches.Ldarg(1),
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("state"),
					ILMatches.Ldarg(0),
					ILMatches.Call("Update"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler_PushForceStackedObject))),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrainCardActions_Transpiler_PopForceStackedObject))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void Combat_DrainCardActions_Transpiler_PushForceStackedObject(Combat combat)
	{
		if (combat.currentCardAction is not { } action)
			return;
		PushForceStackedObject(combat, action);
	}

	private static void Combat_DrainCardActions_Transpiler_PopForceStackedObject(Combat combat)
		=> PopForceStackedObject(combat);
}