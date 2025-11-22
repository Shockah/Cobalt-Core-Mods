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
	private static void HandleRendering()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDrones)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrawIntentLinesForPart)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrawIntentLinesForPart_Transpiler))
		);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_RenderDrones_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			var oldRectLocal = il.DeclareLocal(typeof(Rect));
			
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).GetLocalIndex(out var objectLocalIndex),
					ILMatches.Call("GetGetRect"),
				])
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).ExtractLabels(out var labels),
					ILMatches.Ldarg(1),
					ILMatches.Ldloc<Box>(originalMethod).GetLocalIndex(out var boxLocalIndex),
					ILMatches.Ldflda("rect"),
					ILMatches.Call("get_xy"),
					ILMatches.Call("Render"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler_RenderStackedObjects))),
					
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(Box), nameof(Box.rect))),
					new CodeInstruction(OpCodes.Stloc, oldRectLocal),
					
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler_OffsetMainObject))),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, boxLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldloc, oldRectLocal),
					new CodeInstruction(OpCodes.Stfld, AccessTools.DeclaredField(typeof(Box), nameof(Box.rect))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static double GetWobbleOffset(G g, int depth)
		=> Math.Sin((g.state.time + depth) * 1.5) * 2;

	private static void Combat_RenderDrones_Transpiler_RenderStackedObjects(G g, Box box, StuffBase @object)
	{
		if (GetStackedObjects(@object) is not { } stackedObjects || stackedObjects.Count == 0)
			return;
		var isWobbly = IsWobbly(@object);

		for (var i = 0; i < stackedObjects.Count; i++)
		{
			var offset = isWobbly ? GetWobbleOffset(g, i + 1) : 0;
			stackedObjects[i].Render(g, new Vec(box.rect.x + offset + ((stackedObjects.Count - i) % 2 * 2 - 1) * 2, box.rect.y - stackedObjects.Count + (stackedObjects.Count - i) * 4));
		}
		
		if (box.rect.x is > 60 and < 464 && box.IsHover())
		{
			var tooltipPos = box.rect.xy + new Vec(16, 24);
			g.tooltips.Add(tooltipPos, MakeStackedMidrowAttributeTooltip(stackedObjects.Count + 1));
			if (IsWobbly(@object))
				g.tooltips.Add(tooltipPos, MakeWobblyMidrowAttributeTooltip());
			g.tooltips.Add(tooltipPos, ((IEnumerable<StuffBase>)stackedObjects).Reverse().SelectMany(stackedObject => stackedObject.GetTooltips()));
		}
	}

	private static void Combat_RenderDrones_Transpiler_OffsetMainObject(G g, Box box, StuffBase @object)
	{
		if (GetStackedObjects(@object) is not { } stackedObjects || stackedObjects.Count == 0)
			return;
		var offset = IsWobbly(@object) ? GetWobbleOffset(g, 0) : 0;
		box.rect = new(box.rect.x + offset, box.rect.y - stackedObjects.Count, box.rect.w, box.rect.h);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_DrawIntentLinesForPart_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).GetLocalIndex(out var objectLocalIndex).ExtractLabels(out var labels),
					ILMatches.Isinst<Missile>(),
					ILMatches.Brfalse.GetBranchTarget(out var renderDroneEndCapLabel),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrawIntentLinesForPart_Transpiler_IsStackBlocking))),
					new CodeInstruction(OpCodes.Brtrue, renderDroneEndCapLabel.Value),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static bool Combat_DrawIntentLinesForPart_Transpiler_IsStackBlocking(StuffBase @object, Ship shipSource)
	{
		if (GetStackedObjects(@object) is not { } stackedObjects || stackedObjects.Count == 0)
			return false;
		if (shipSource.isPlayerShip)
			return true;
		// TODO: Jack compat, if needed
		foreach (var stackedObject in stackedObjects)
			if (stackedObject is not Missile)
				return true;
		return false;
	}
}