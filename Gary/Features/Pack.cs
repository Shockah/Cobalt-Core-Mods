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

	private static StuffBase? ObjectBeingPackedInto;
	
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
	}

	internal static List<StuffBase>? GetPackedObjects(StuffBase @object)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<List<StuffBase>>(@object, "PackedObjects");

	internal static void PushPackedObject(Combat combat, int worldX, StuffBase pushed)
	{
		ref var @object = ref CollectionsMarshal.GetValueRefOrAddDefault(combat.stuff, worldX, out var objectExists);
		if (!objectExists)
		{
			@object = pushed;
			return;
		}

		List<StuffBase> packedObjects = [
			.. GetPackedObjects(pushed) ?? [],
			@object!,
			.. GetPackedObjects(@object!) ?? [],
		];
		ModEntry.Instance.Helper.ModData.RemoveModData(@object!, "PackedObjects");
		ModEntry.Instance.Helper.ModData.SetModData(pushed, "PackedObjects", packedObjects);
		@object = pushed;
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
	
	private static void ASpawn_Begin_Prefix(ASpawn __instance, State s, Combat c, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		
		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;
		
		var packAmount = ship.Get(PackStatus.Status);
		var cramAmount = ship.Get(CramStatus.Status);
		if (packAmount <= 0)
			return;

		var worldX = __instance.GetWorldX(s, c);
		if (!c.stuff.TryGetValue(worldX, out var existingThing))
			return;

		var packSize = 1 + (GetPackedObjects(existingThing)?.Count ?? 0);
		if (packSize > packAmount + cramAmount)
			return;
		
		var packToRemove = packSize - cramAmount;
		ship.Add(PackStatus.Status, -packToRemove);

		ObjectBeingPackedInto = existingThing;
		c.stuff.Remove(worldX);
	}

	private static void ASpawn_Begin_Finalizer(ASpawn __instance, State s, Combat c)
	{
		if (ObjectBeingPackedInto is null)
			return;
		
		var ship = __instance.fromPlayer ? s.ship : c.otherShip;
		if (ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;
		
		var worldX = __instance.GetWorldX(s, c);
		var existingObject = c.stuff.GetValueOrDefault(worldX);

		c.stuff.Remove(worldX);
		PushPackedObject(c, worldX, ObjectBeingPackedInto);
		if (existingObject is not null)
			PushPackedObject(c, worldX, existingObject);
		
		ObjectBeingPackedInto = null;
	}

	private static void Combat_DestroyDroneAt_Prefix(Combat __instance, int x, out StuffBase? __state)
		=> __state = __instance.stuff.GetValueOrDefault(x);

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
		
		actions.InsertRange(0, packedObjects.SelectMany(packedObject => packedObject.GetActions(state, combat) ?? []));
		return actions;
	}
}