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

internal sealed class CramManager : IRegisterable
{
	internal static IStatusEntry CramStatus { get; private set; } = null!;
	internal static IStatusEntry CramHarderStatus { get; private set; } = null!;

	private static StuffBase? ObjectBeingLaunchedInto;
	private static StuffBase? ObjectToPutLater;
	private static bool ObjectIsBeingCrammedInto;
	private static Guid? NestedJupiterShootBeginId;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
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
		
		CramHarderStatus = ModEntry.Instance.Helper.Content.Statuses.RegisterStatus("CramHarder", new()
		{
			Definition = new()
			{
				icon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Status/CramHarder.png")).Sprite,
				color = new("23EEB6"),
				isGood = true,
			},
			Name = ModEntry.Instance.AnyLocalizations.Bind(["status", "CramHarder", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["status", "CramHarder", "description"]).Localize
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
			original: AccessTools.DeclaredMethod(typeof(ADroneTurn), nameof(ADroneTurn.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneTurn_GetTooltips_Postfix))
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
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_GetTooltips_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.DoWeHaveCannonsThough)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_DoWeHaveCannonsThough_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AJupiterShoot), nameof(AJupiterShoot.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AJupiterShoot_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AJupiterShoot_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(JupiterDroneHubV2), nameof(JupiterDroneHubV2.OnPlayerSpawnSomething)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(JupiterDroneHubV2_OnPlayerSpawnSomething_Prefix))
		);
	}

	internal static List<StuffBase>? GetCrammedObjects(StuffBase @object)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<List<StuffBase>>(@object, "CrammedObjects");

	internal static void PushCrammedObject(Combat combat, int worldX, StuffBase pushed)
	{
		ref var @object = ref CollectionsMarshal.GetValueRefOrAddDefault(combat.stuff, worldX, out var objectExists);
		if (!objectExists)
		{
			Put();
			return;
		}

		List<StuffBase> crammedObjects = [
			.. GetCrammedObjects(pushed) ?? [],
			@object!,
			.. GetCrammedObjects(@object!) ?? [],
		];
		ModEntry.Instance.Helper.ModData.RemoveModData(@object!, "CrammedObjects");
		ModEntry.Instance.Helper.ModData.SetModData(pushed, "CrammedObjects", crammedObjects);

		Put();

		void Put()
		{
			if (ObjectBeingLaunchedInto is not null && !ObjectIsBeingCrammedInto)
				ObjectToPutLater = pushed;
			else
				combat.stuff[worldX] = pushed;

			UpdateCrammedObjectX(pushed, worldX, true);
		}
	}

	internal static bool PopCrammedObject(Combat combat, int worldX, bool removeLast)
	{
		if (!combat.stuff.Remove(worldX, out var @object))
			return false;
		if (GetCrammedObjects(@object) is not { } crammedObjects || crammedObjects.Count == 0)
		{
			if (!removeLast)
				combat.stuff[worldX] = @object;
			return removeLast;
		}

		ModEntry.Instance.Helper.ModData.RemoveModData(@object, "CrammedObjects");
		@object = crammedObjects[^1];

		crammedObjects = crammedObjects.Count == 0 ? null : crammedObjects.Take(crammedObjects.Count - 1).ToList();
		ModEntry.Instance.Helper.ModData.SetOptionalModData(@object, "CrammedObjects", crammedObjects);
		combat.stuff[worldX] = @object;
		return true;
	}

	internal static bool RemoveCrammedObject(Combat combat, int worldX, StuffBase toRemove)
	{
		if (!combat.stuff.Remove(worldX, out var @object))
			return false;

		if (@object == toRemove)
		{
			if (GetCrammedObjects(@object) is { } crammedObjects && crammedObjects.Count != 0)
			{
				ModEntry.Instance.Helper.ModData.RemoveModData(@object, "CrammedObjects");
				@object = crammedObjects[^1];
				
				crammedObjects = crammedObjects.Count == 0 ? null : crammedObjects.Take(crammedObjects.Count - 1).ToList();
				ModEntry.Instance.Helper.ModData.SetOptionalModData(@object, "CrammedObjects", crammedObjects);
				combat.stuff[worldX] = @object;

				return true;
			}
			
			combat.stuff.Remove(worldX);
			return true;
		}
		
		if (GetCrammedObjects(@object) is { } crammedObjects2 && crammedObjects2.Count != 0)
			return crammedObjects2.Remove(toRemove);
		return false;
	}

	private static void UpdateCrammedObjectX(StuffBase @object, int? maybeWorldX = null, bool updateXLerped = false)
	{
		var worldX = maybeWorldX ?? @object.x;
		@object.x = worldX;
		if (updateXLerped)
			@object.xLerped = worldX;

		if (GetCrammedObjects(@object) is { } crammedObjects)
		{
			foreach (var crammedObject in crammedObjects)
			{
				crammedObject.x = worldX;
				if (updateXLerped)
					crammedObject.xLerped = worldX;
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
		
		var cramAmount = ship.Get(CramStatus.Status);
		var cramHarderAmount = ship.Get(CramHarderStatus.Status);
		if (cramAmount <= 0)
			return;

		var stackSize = 1 + (GetCrammedObjects(existingThing)?.Count ?? 0);
		if (stackSize > cramAmount + cramHarderAmount)
			return;
		
		var cramToRemove = stackSize - cramHarderAmount;
		ship.Add(CramStatus.Status, -cramToRemove);

		ObjectIsBeingCrammedInto = true;
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

		if (ObjectIsBeingCrammedInto)
		{
			c.stuff.Remove(worldX);
			PushCrammedObject(c, worldX, ObjectBeingLaunchedInto);
			if (existingObject is not null)
				PushCrammedObject(c, worldX, existingObject);
			ObjectIsBeingCrammedInto = false;
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
			UpdateCrammedObjectX(@object, x);
	}

	private static void Combat_DestroyDroneAt_Postfix(Combat __instance, int x, in StuffBase? __state)
	{
		if (__state is null)
			return;

		if (__instance.stuff.Remove(x, out var existingThing))
		{
			PushCrammedObject(__instance, x, __state);
			PushCrammedObject(__instance, x, existingThing);
			return;
		}

		if (GetCrammedObjects(__state) is not { } crammedObjects || crammedObjects.Count == 0)
			return;
		
		ModEntry.Instance.Helper.ModData.RemoveModData(__state, "CrammedObjects");
		
		var newObject = crammedObjects[^1];
		crammedObjects = crammedObjects.Count == 0 ? null : crammedObjects.Take(crammedObjects.Count - 1).ToList();
		ModEntry.Instance.Helper.ModData.SetOptionalModData(newObject, "CrammedObjects", crammedObjects);
		
		PushCrammedObject(__instance, x, newObject);
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
		if (GetCrammedObjects(existingThing) is not { } crammedObjects || crammedObjects.Count == 0)
			return actions;
		
		UpdateCrammedObjectX(existingThing, worldX);
		actions.InsertRange(0, crammedObjects.SelectMany(crammedObject =>
		{
			if (crammedObject.GetActions(state, combat) is not { } actions)
				return [];
			
			var crammedObjectId = ModEntry.Instance.Helper.ModData.ObtainModData(crammedObject, "CrammedObjectId", Guid.NewGuid);
			return actions
				.Select(a =>
				{
					ModEntry.Instance.Helper.ModData.SetModData(a, "ForceCrammedObjectId", crammedObjectId);
					ModEntry.Instance.Helper.ModData.SetModData(a, "ForceCrammedObjectWorldX", worldX);
					return a;
				});
		}));
		return actions;
	}

	private static void ADroneTurn_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		foreach (var @object in combat.stuff.Values)
			if (GetCrammedObjects(@object) is { } crammedObjects)
				foreach (var crammedObject in crammedObjects)
					if (crammedObject.GetActions(s, combat) is not null)
						crammedObject.hilight = 2;
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDrones_Transpiler_RenderCrammedObjects))),
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
		if (GetCrammedObjects(@object) is not { } crammedObjects || crammedObjects.Count == 0)
			return rect;
		return new(rect.x, rect.y - crammedObjects.Count, rect.w, rect.h);
	}

	private static void Combat_RenderDrones_Transpiler_RenderCrammedObjects(G g, Box box, StuffBase @object)
	{
		if (GetCrammedObjects(@object) is not { } crammedObjects || crammedObjects.Count == 0)
			return;

		for (var i = 0; i < crammedObjects.Count; i++)
			crammedObjects[i].Render(g, new Vec(box.rect.x + ((crammedObjects.Count - i) % 2 * 2 - 1) * 2, box.rect.y - crammedObjects.Count + (crammedObjects.Count - i) * 4));
		if (box.rect.x is > 60 and < 464 && box.IsHover())
			g.tooltips.Add(box.rect.xy + new Vec(16, 24), ((IEnumerable<StuffBase>)crammedObjects).Reverse().SelectMany(crammedObject => crammedObject.GetTooltips()));
	}

	private static void Combat_ResetHilights_Postfix(Combat __instance)
	{
		foreach (var @object in __instance.stuff.Values)
			if (GetCrammedObjects(@object) is { } crammedObjects)
				foreach (var crammedObject in crammedObjects)
					if (crammedObject.hilight > 0)
						crammedObject.hilight--;
	}

	private static void StuffBase_Update_Postfix(StuffBase __instance, G g)
	{
		if (GetCrammedObjects(__instance) is { } crammedObjects)
			foreach (var crammedObject in crammedObjects)
				crammedObject.Update(g);
	}

	private static void Combat_BeginCardAction_Prefix(Combat __instance, CardAction a, out (StuffBase RealObject, StuffBase CrammedObject, int WorldX)? __state)
	{
		__state = null;
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(a, "ForceCrammedObjectId", out var forceCrammedObjectId))
			return;
		if (!ModEntry.Instance.Helper.ModData.TryGetModData<int>(a, "ForceCrammedObjectWorldX", out var forceCrammedObjectWorldX))
			return;
		if (!__instance.stuff.TryGetValue(forceCrammedObjectWorldX, out var @object))
			return;
		if (GetCrammedObjects(@object) is not { } crammedObjects)
			return;
		if (crammedObjects.FirstOrDefault(crammedObject => ModEntry.Instance.Helper.ModData.GetOptionalModData<Guid>(crammedObject, "CrammedObjectId") == forceCrammedObjectId) is not { } crammedObject)
			return;

		__state = (@object, crammedObject, forceCrammedObjectWorldX);
		__instance.stuff[forceCrammedObjectWorldX] = crammedObject;
	}

	private static void Combat_BeginCardAction_Finalizer(Combat __instance, in (StuffBase RealObject, StuffBase CrammedObject, int WorldX)? __state)
	{
		if (__state is not { } e)
			return;

		var existingObject = __instance.stuff.GetValueOrDefault(e.WorldX);
		__instance.stuff[e.WorldX] = e.RealObject;
		if (existingObject != e.CrammedObject)
			RemoveCrammedObject(__instance, e.WorldX, e.CrammedObject);
	}

	private static void AAttack_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;

		foreach (var @object in combat.stuff.Values)
		{
			if (GetCrammedObjects(@object) is not { } crammedObjects)
				continue;
			foreach (var crammedObject in crammedObjects)
				if (crammedObject is JupiterDrone)
					crammedObject.hilight = 2;
		}
	}

	private static void AAttack_DoWeHaveCannonsThough_Postfix(State s, ref bool __result)
	{
		if (__result)
			return;
		if (s.route is not Combat combat)
			return;
		
		foreach (var @object in combat.stuff.Values)
		{
			if (GetCrammedObjects(@object) is not { } crammedObjects)
				continue;
			foreach (var crammedObject in crammedObjects)
			{
				if (crammedObject is not JupiterDrone)
					continue;
				__result = true;
				return;
			}
		}
	}

	private static void AJupiterShoot_Begin_Prefix(AJupiterShoot __instance, out Guid __state)
	{
		__state = NestedJupiterShootBeginId ?? Guid.NewGuid();
		ModEntry.Instance.Helper.ModData.SetModData(__instance.attackCopy, "IsFromAJupiterShoot", __state);
	}

	private static void AJupiterShoot_Begin_Postfix(AJupiterShoot __instance, G g, State s, Combat c, in Guid __state)
	{
		if (NestedJupiterShootBeginId is not null)
			return;
		
		List<(int WorldX, AAttack Attack, StuffBase TopObject, StuffBase? CrammedObject, int Depth)> attacks = [];
		for (var i = c.cardActions.Count - 1; i >= 0; i--)
		{
			if (c.cardActions[i] is not AAttack attack)
				continue;
			if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(attack, "IsFromAJupiterShoot", out var instanceId))
				continue;
			if (instanceId != __state)
				continue;
			if (attack.fromDroneX is null || !c.stuff.TryGetValue(attack.fromDroneX.Value, out var @object))
				continue;

			attacks.Add((attack.fromDroneX.Value, attack, @object, null, 0));
			c.cardActions.RemoveAt(i);
		}

		NestedJupiterShootBeginId = __state;
		var realStuff = c.stuff;
		
		try
		{
			List<(int WorldX, StuffBase TopObject, JupiterDrone JupiterDrone, int Depth)> crammedJupitedDrones = [];

			foreach (var kvp in c.stuff)
				if (GetCrammedObjects(kvp.Value) is { } crammedObjects)
					for (var i = 0; i < crammedObjects.Count; i++)
						if (crammedObjects[i] is JupiterDrone jupiterDrone)
							crammedJupitedDrones.Add((kvp.Key, kvp.Value, jupiterDrone, i + 1));
				
			c.stuff = [];

			while (crammedJupitedDrones.Count != 0)
			{
				var entry = crammedJupitedDrones[^1];
				crammedJupitedDrones.RemoveAt(crammedJupitedDrones.Count - 1);
				c.stuff[entry.WorldX] = entry.JupiterDrone;
				
				__instance.Begin(g, s, c);
				
				for (var i = c.cardActions.Count - 1; i >= 0; i--)
				{
					if (c.cardActions[i] is not AAttack attack)
						continue;
					if (!ModEntry.Instance.Helper.ModData.TryGetModData<Guid>(attack, "IsFromAJupiterShoot", out var instanceId))
						continue;
					if (instanceId != __state)
						continue;
					if (attack.fromDroneX is null || !c.stuff.ContainsKey(attack.fromDroneX.Value))
						continue;

					var crammedObjectId = ModEntry.Instance.Helper.ModData.ObtainModData(entry.JupiterDrone, "CrammedObjectId", Guid.NewGuid);
					ModEntry.Instance.Helper.ModData.SetModData(attack, "ForceCrammedObjectId", crammedObjectId);
					ModEntry.Instance.Helper.ModData.SetModData(attack, "ForceCrammedObjectWorldX", entry.WorldX);
					
					attacks.Add((entry.WorldX, attack, entry.TopObject, entry.JupiterDrone, entry.Depth));
					c.cardActions.RemoveAt(i);
				}
			}
		}
		finally
		{
			c.stuff = realStuff;
			NestedJupiterShootBeginId = null;
		}
		
		c.QueueImmediate(
			attacks
				.OrderBy(e => e.WorldX)
				.ThenBy(e => e.Depth)
				.Select(e => e.Attack)
		);
	}

	private static bool JupiterDroneHubV2_OnPlayerSpawnSomething_Prefix(Combat combat, StuffBase thing)
	{
		if (thing is not JupiterDrone)
			return true;
		
		foreach (var @object in combat.stuff.Values)
			if (GetCrammedObjects(@object) is { } crammedObjects)
				if (crammedObjects.Any(crammedObject => crammedObject is JupiterDrone))
					return false;

		return true;
	}
}