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
	private static void HandleLaunch()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_GetTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler))
		);
	}
	
	private static void ASpawn_Begin_Prefix(ASpawn __instance, State s, Combat c, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		
		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;

		var worldX = __instance.GetWorldX(s, c) + __instance.offset;
		if (!c.stuff.TryGetValue(worldX, out var existingThing))
			return;
		ObjectBeingLaunchedInto = existingThing;

		var willStack = IsStacked(__instance);

		if (!willStack)
		{
			var stackSize = 1 + (GetStackedObjects(existingThing)?.Count ?? 0);
			var jengaAmount = ship.Get(ApmStatus.Status);

			if (jengaAmount > 0)
			{
				willStack = true;
				ship.Add(ApmStatus.Status, -Math.Min(jengaAmount, stackSize));
				if (stackSize > jengaAmount)
					SetWobbly(__instance.thing);
			}
		}

		if (!willStack)
			return;

		ObjectIsBeingStackedInto = true;
		c.stuff.Remove(worldX);
	}

	private static void ASpawn_Begin_Finalizer(ASpawn __instance, State s, Combat c)
	{
		if (ObjectBeingLaunchedInto is null)
			return;
		
		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;
		
		var worldX = __instance.GetWorldX(s, c) + __instance.offset;
		var existingObject = c.stuff.GetValueOrDefault(worldX);

		if (ObjectIsBeingStackedInto)
		{
			c.stuff.Remove(worldX);
			PushStackedObject(c, worldX, ObjectBeingLaunchedInto);
			if (existingObject is not null)
				PushStackedObject(c, worldX, existingObject);
			ObjectIsBeingStackedInto = false;
		}
		else if (ObjectToPutLater is not null)
		{
			c.stuff[worldX] = ObjectToPutLater;
			ObjectToPutLater = null;
		}
		
		ObjectBeingLaunchedInto = null;
	}

	private static void ASpawn_GetTooltips_Postfix(ASpawn __instance, ref List<Tooltip> __result)
	{
		if (IsWobbly(__instance.thing))
		{
			List<Tooltip> tooltipsToInsert = [
				MakeStackedLaunchTooltip(),
				MakeStackedMidrowAttributeTooltip(),
				MakeWobblyMidrowAttributeTooltip(),
			];
		
			var index = __result.FindIndex(t => t is TTGlossary { key: "action.spawn" or "action.spawnOffsetLeft" or "action.spawnOffsetRight" });
			__result.InsertRange(index == -1 ? 0 : (index + 1), tooltipsToInsert);
		}
		else if (IsStacked(__instance))
		{
			List<Tooltip> tooltipsToInsert = [
				MakeStackedLaunchTooltip(),
				MakeStackedMidrowAttributeTooltip(),
			];
		
			var index = __result.FindIndex(t => t is TTGlossary { key: "action.spawn" or "action.spawnOffsetLeft" or "action.spawnOffsetRight" });
			__result.InsertRange(index == -1 ? 0 : (index + 1), tooltipsToInsert);
		}
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Card_RenderAction_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.AnyLdloca,
					ILMatches.Ldarg(3),
					ILMatches.Stfld("dontDraw").SelectElement(out var dontDrawField, i => (FieldInfo)i.operand!),
				])
				.Find([
					ILMatches.AnyLdloc.GetLocalIndex(out var capturesLocalIndex),
					ILMatches.Ldfld("w").SelectElement(out var wField, i => (FieldInfo)i.operand!),
				])
				.Find([
					ILMatches.Ldloc<ASpawn>(originalMethod).GetLocalIndex(out var actionLocalIndex),
					ILMatches.AnyLdloca,
					new ElementMatch<CodeInstruction>($"{{(any) call to local method named SpawnIcon}}", i => ILMatches.AnyCall.Matches(i) && (i.operand as MethodBase)?.Name.StartsWith("<RenderAction>g__SpawnIcon") == true),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, actionLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldloc, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldfld, dontDrawField.Value),
					new CodeInstruction(OpCodes.Ldloca, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldflda, wField.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler_RenderStackedLaunch))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void Card_RenderAction_Transpiler_RenderStackedLaunch(ASpawn action, G g, bool dontDraw, ref int width)
	{
		const int leftPad = -2;

		var isWobbly = IsWobbly(action.thing);
		var isStacked = isWobbly || IsStacked(action);
		
		if (isStacked)
		{
			var stackedBox = g.Push(rect: new Rect(width + leftPad));
			if (!dontDraw)
				Draw.Sprite(StackedLaunchIcon.Sprite, stackedBox.rect.x, stackedBox.rect.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
			width += 9 + leftPad;
			g.Pop();
			
			if (isWobbly)
			{
				var wobblyBox = g.Push(rect: new Rect(width + leftPad));
				if (!dontDraw)
					Draw.Sprite(WobblyIcon.Sprite, wobblyBox.rect.x, wobblyBox.rect.y, color: action.disabled ? Colors.disabledIconTint : Colors.white);
				width += 8;
				g.Pop();
			}
		}
	}
}