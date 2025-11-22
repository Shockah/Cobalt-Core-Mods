using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Shockah.Gary;

internal partial class Stack
{
	private static Guid? NestedJupiterShootBeginId;
	
	private static void HandleJupiterDrones()
	{
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
	
	private static void AAttack_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllStackedObjects(combat, @object =>
		{
			if (@object is JupiterDrone)
				@object.hilight = 2;
		});
	}

	private static void AAttack_DoWeHaveCannonsThough_Postfix(State s, ref bool __result)
	{
		if (__result)
			return;
		if (s.route is not Combat combat)
			return;
		
		if (AnyStackedObject(combat, @object => @object is JupiterDrone))
			__result = true;
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
		
		List<(int WorldX, AAttack Attack, StuffBase TopObject, StuffBase? StackedObject, int Depth)> attacks = [];
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
			List<(int WorldX, StuffBase TopObject, JupiterDrone JupiterDrone, int Depth)> stackedJupitedDrones = [];

			foreach (var kvp in c.stuff)
				if (GetStackedObjects(kvp.Value) is { } stackedObjects)
					for (var i = 0; i < stackedObjects.Count; i++)
						if (stackedObjects[i] is JupiterDrone jupiterDrone)
							stackedJupitedDrones.Add((kvp.Key, kvp.Value, jupiterDrone, i + 1));
				
			c.stuff = [];

			while (stackedJupitedDrones.Count != 0)
			{
				var entry = stackedJupitedDrones[^1];
				stackedJupitedDrones.RemoveAt(stackedJupitedDrones.Count - 1);
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

					var stackedObjectId = ObtainStackedObjectId(entry.JupiterDrone);
					ModEntry.Instance.Helper.ModData.SetModData(attack, "ForceStackedObjectId", stackedObjectId);
					ModEntry.Instance.Helper.ModData.SetModData(attack, "ForceStackedObjectWorldX", entry.WorldX);
					
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

		if (AnyStackedObject(combat, @object => @object is JupiterDrone))
			return false;

		return true;
	}
}