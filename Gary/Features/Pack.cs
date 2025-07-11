using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.Gary;

internal sealed class PackManager : IRegisterable
{
	internal static IStatusEntry PackStatus { get; private set; } = null!;
	internal static IStatusEntry CramStatus { get; private set; } = null!;

	private static StuffBase? ObjectBeingLaunchedInto;
	private static StuffBase? ObjectToPutLater;
	private static bool ObjectIsBeingPackedInto;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		PackStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Pack", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Pack.png")).Sprite,
				color = new("23EEB6"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Pack", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Pack", "description"]).Localize
		});
		
		CramStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("Cram", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/Cram.png")).Sprite,
				color = new("23EEB6"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "Cram", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "Cram", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DestroyDroneAt)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DestroyDroneAt_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DestroyDroneAt_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADroneTurn), nameof(ADroneTurn.Begin)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneTurn_Begin_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDrones)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.ResetHilights)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_ResetHilights_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.Update)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StuffBase_Update_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.BeginCardAction)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_BeginCardAction_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_BeginCardAction_Finalizer))
		);
	}

	internal static List<StuffBase>? GetPackedObjects(StuffBase @object)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<List<StuffBase>>(@object, "PackedObjects");

	internal static void PushPackedObject(Combat combat, int worldX, StuffBase pushed)
	{
		ref var @object = ref CollectionsMarshal.GetValueRefOrAddDefault(combat.stuff, worldX, out var objectExists);
		if (!objectExists)
		{
			Put();
			return;
		}

		List<StuffBase> packedObjects = [
			.. GetPackedObjects(pushed) ?? [],
			@object!,
			.. GetPackedObjects(@object!) ?? [],
		];
		ModEntry.Instance.Helper.ModData.RemoveModData(@object!, "PackedObjects");
		ModEntry.Instance.Helper.ModData.SetModData(pushed, "PackedObjects", packedObjects);

		Put();

		void Put()
		{
			if (ObjectBeingLaunchedInto is not null && !ObjectIsBeingPackedInto)
				ObjectToPutLater = pushed;
			else
				combat.stuff[worldX] = pushed;

			UpdatePackedObjectX(pushed, worldX, true);
		}
	}

	internal static bool PopPackedObject(Combat combat, int worldX, bool removeLast)
	{
		if (!combat.stuff.Remove(worldX, out var @object))
			return false;
		if (GetPackedObjects(@object) is not { } packedObjects || packedObjects.Count == 0)
		{
			if (!removeLast)
				combat.stuff[worldX] = @object;
			return removeLast;
		}

		ModEntry.Instance.Helper.ModData.RemoveModData(@object, "PackedObjects");
		@object = packedObjects[^1];

		packedObjects = packedObjects.Count == 0 ? null : packedObjects.Take(packedObjects.Count - 1).ToList();
		ModEntry.Instance.Helper.ModData.SetOptionalModData(@object, "PackedObjects", packedObjects);
		combat.stuff[worldX] = @object;
		return true;
	}

	internal static bool RemovePackedObject(Combat combat, int worldX, StuffBase toRemove)
	{
		if (!combat.stuff.Remove(worldX, out var @object))
			return false;

		if (@object == toRemove)
		{
			if (GetPackedObjects(@object) is { } packedObjects && packedObjects.Count != 0)
			{
				ModEntry.Instance.Helper.ModData.RemoveModData(@object, "PackedObjects");
				@object = packedObjects[^1];
				
				packedObjects = packedObjects.Count == 0 ? null : packedObjects.Take(packedObjects.Count - 1).ToList();
				ModEntry.Instance.Helper.ModData.SetOptionalModData(@object, "PackedObjects", packedObjects);
				combat.stuff[worldX] = @object;

				return true;
			}
			
			combat.stuff.Remove(worldX);
			return true;
		}
		
		if (GetPackedObjects(@object) is { } packedObjects2 && packedObjects2.Count != 0)
			return packedObjects2.Remove(toRemove);
		return false;
	}

	private static void UpdatePackedObjectX(StuffBase @object, int? maybeWorldX = null, bool updateXLerped = false)
	{
		var worldX = maybeWorldX ?? @object.x;
		@object.x = worldX;
		if (updateXLerped)
			@object.xLerped = worldX;

		if (GetPackedObjects(@object) is { } packedObjects)
		{
			foreach (var packedObject in packedObjects)
			{
				packedObject.x = worldX;
				if (updateXLerped)
					packedObject.xLerped = worldX;
			}
		}
	}
	
	private static void ASpawn_Begin_Prefix(ASpawn __instance, State s, Combat c, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		
		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;

		var worldX = __instance.GetWorldX(s, c);
		if (!c.stuff.TryGetValue(worldX, out var existingThing))
			return;
		ObjectBeingLaunchedInto = existingThing;
		
		var packAmount = ship.Get(PackStatus.Status);
		var cramAmount = ship.Get(CramStatus.Status);
		if (packAmount <= 0)
			return;

		var packSize = 1 + (GetPackedObjects(existingThing)?.Count ?? 0);
		if (packSize > packAmount + cramAmount)
			return;
		
		var packToRemove = packSize - cramAmount;
		ship.Add(PackStatus.Status, -packToRemove);

		ObjectIsBeingPackedInto = true;
		c.stuff.Remove(worldX);
	}

	private static void ASpawn_Begin_Finalizer(ASpawn __instance, State s, Combat c)
	{
		if (ObjectBeingLaunchedInto is null)
			return;
		
		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;
		
		var worldX = __instance.GetWorldX(s, c);
		var existingObject = c.stuff.GetValueOrDefault(worldX);

		if (ObjectIsBeingPackedInto)
		{
			c.stuff.Remove(worldX);
			PushPackedObject(c, worldX, ObjectBeingLaunchedInto);
			if (existingObject is not null)
				PushPackedObject(c, worldX, existingObject);
			ObjectIsBeingPackedInto = false;
		}
		else if (ObjectToPutLater is not null)
		{
			c.stuff[worldX] = ObjectToPutLater;
			ObjectToPutLater = null;
		}
		
		ObjectBeingLaunchedInto = null;
	}

	private static void Combat_DestroyDroneAt_Prefix(Combat __instance, int x, out StuffBase? __state)
	{
		__state = __instance.stuff.GetValueOrDefault(x);
		if (__state is { } @object)
			UpdatePackedObjectX(@object, x);
	}

	private static void Combat_DestroyDroneAt_Postfix(Combat __instance, int x, in StuffBase? __state)
	{
		if (__state is null)
			return;

		if (__instance.stuff.Remove(x, out var existingThing))
		{
			PushPackedObject(__instance, x, __state);
			PushPackedObject(__instance, x, existingThing);
			return;
		}

		if (GetPackedObjects(__state) is not { } packedObjects || packedObjects.Count == 0)
			return;
		
		ModEntry.Instance.Helper.ModData.RemoveModData(__state, "PackedObjects");
		
		var newObject = packedObjects[^1];
		packedObjects = packedObjects.Count == 0 ? null : packedObjects.Take(packedObjects.Count - 1).ToList();
		ModEntry.Instance.Helper.ModData.SetOptionalModData(newObject, "PackedObjects", packedObjects);
		
		PushPackedObject(__instance, x, newObject);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> ADroneTurn_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(3),
					ILMatches.Ldfld("stuff"),
					ILMatches.Ldloc<int>(originalMethod).GetLocalIndex(out var worldXLocalIndex),
					ILMatches.Call("get_Item"),
					ILMatches.Ldarg(2),
					ILMatches.Ldarg(3),
					ILMatches.Call("GetActions"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldloc, worldXLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneTurn_Begin_Transpiler_ModifyActions))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static List<CardAction> ADroneTurn_Begin_Transpiler_ModifyActions(List<CardAction> actions, State state, Combat combat, int worldX)
	{
		if (!combat.stuff.TryGetValue(worldX, out var existingThing))
			return actions;
		if (GetPackedObjects(existingThing) is not { } packedObjects || packedObjects.Count == 0)
			return actions;
		
		UpdatePackedObjectX(existingThing, worldX);
		actions.InsertRange(0, packedObjects.SelectMany(packedObject =>
		{
			if (packedObject.GetActions(state, combat) is not { } actions)
				return [];
			
			var packedObjectId = ModEntry.Instance.Helper.ModData.ObtainModData(packedObject, "PackedObjectId", Guid.NewGuid);
			return actions
				.Select(a =>
				{
					ModEntry.Instance.Helper.ModData.SetModData(a, "ForcePackedObjectId", packedObjectId);
					ModEntry.Instance.Helper.ModData.SetModData(a, "ForcePackedObjectWorldX", worldX);
					return a;
				});
		}));
		return actions;
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_RenderDrones_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod).GetLocalIndex(out var objectLocalIndex),
					ILMatches.Call("GetGetRect"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloc, objectLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler_OffsetMainObject))),
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler_RenderPackedObjects))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static Rect Combat_RenderDrones_Transpiler_OffsetMainObject(Rect rect, StuffBase @object)
	{
		if (GetPackedObjects(@object) is not { } packedObjects || packedObjects.Count == 0)
			return rect;
		return new(rect.x, rect.y - packedObjects.Count, rect.w, rect.h);
	}

	private static void Combat_RenderDrones_Transpiler_RenderPackedObjects(G g, Box box, StuffBase @object)
	{
		if (GetPackedObjects(@object) is not { } packedObjects || packedObjects.Count == 0)
			return;

		for (var i = 0; i < packedObjects.Count; i++)
			packedObjects[i].Render(g, new Vec(box.rect.x + ((packedObjects.Count - i) % 2 * 2 - 1) * 2, box.rect.y - packedObjects.Count + (packedObjects.Count - i) * 4));
		if (box.rect.x is > 60 and < 464 && box.IsHover())
			g.tooltips.Add(box.rect.xy + new Vec(16, 24), ((IEnumerable<StuffBase>)packedObjects).Reverse().SelectMany(packedObject => packedObject.GetTooltips()));
	}

	private static void Combat_ResetHilights_Postfix(Combat __instance)
	{
		foreach (var @object in __instance.stuff.Values)
			if (GetPackedObjects(@object) is { } packedObjects)
				foreach (var packedObject in packedObjects)
					if (packedObject.hilight > 0)
						packedObject.hilight--;
	}

	private static void StuffBase_Update_Postfix(StuffBase __instance, G g)
	{
		if (GetPackedObjects(__instance) is { } packedObjects)
			foreach (var packedObject in packedObjects)
				packedObject.Update(g);
	}

	private static void Combat_BeginCardAction_Prefix(Combat __instance, CardAction a, out (StuffBase RealObject, StuffBase PackedObject, int WorldX)? __state)
	{
		__state = null;
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(a, "ForcePackedObjectId", out var forcePackedObjectId))
			return;
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<int>(a, "ForcePackedObjectWorldX", out var forcePackedObjectWorldX))
			return;
		if (!__instance.stuff.TryGetValue(forcePackedObjectWorldX, out var @object))
			return;
		if (GetPackedObjects(@object) is not { } packedObjects)
			return;
		if (packedObjects.FirstOrDefault(packedObject => ModEntry.Instance.Helper.ModData.GetOptionalModData<Guid>(packedObject, "PackedObjectId") == forcePackedObjectId) is not { } packedObject)
			return;

		__state = (@object, packedObject, forcePackedObjectWorldX);
		__instance.stuff[forcePackedObjectWorldX] = packedObject;
	}

	private static void Combat_BeginCardAction_Finalizer(Combat __instance, in (StuffBase RealObject, StuffBase PackedObject, int WorldX)? __state)
	{
		if (__state is not { } e)
			return;

		var existingObject = __instance.stuff.GetValueOrDefault(e.WorldX);
		__instance.stuff[e.WorldX] = e.RealObject;
		if (existingObject != e.PackedObject)
			RemovePackedObject(__instance, e.WorldX, e.PackedObject);
	}
}