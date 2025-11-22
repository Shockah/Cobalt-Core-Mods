using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Shockah.Gary;

internal partial class Stack
{
	private static bool IsDuringWobblyDestroy;
	
	private static void HandleDestroy()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DestroyDroneAt)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DestroyDroneAt_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DestroyDroneAt_Postfix))
		);
	}
	
	private static void Combat_DestroyDroneAt_Prefix(Combat __instance, int x, out StuffBase? __state)
	{
		__state = __instance.stuff.GetValueOrDefault(x);
		if (__state is { } @object)
			UpdateStackedObjectX(@object, x);
	}

	private static void Combat_DestroyDroneAt_Postfix(Combat __instance, State s, int x, bool playerDidIt, in StuffBase? __state)
	{
		if (IsDuringWobblyDestroy)
			return;
		if (__state is null)
			return;

		if (__instance.stuff.Remove(x, out var existingThing))
		{
			PushStackedObject(__instance, x, __state);
			PushStackedObject(__instance, x, existingThing);
			return;
		}

		if (GetStackedObjects(__state) is not { } stackedObjects || stackedObjects.Count == 0)
			return;
		
		SetStackedObjects(__state, null);

		if (IsWobbly(__state))
		{
			IsDuringWobblyDestroy = true;
			try
			{
				var newObjects = new List<StuffBase>();
				
				while (stackedObjects.Count != 0)
				{
					var lastStackedObject = stackedObjects[^1];
					stackedObjects.RemoveAt(stackedObjects.Count - 1);
					
					__instance.stuff[x] = lastStackedObject;
					__instance.DestroyDroneAt(s, x, playerDidIt);
					
					if (__instance.stuff.Remove(x, out var existingThing2))
					{
						if (GetStackedObjects(existingThing2) is { } stackedObjects2)
							newObjects.AddRange(stackedObjects2);
						newObjects.Add(existingThing2);
					}
				}

				if (newObjects.Count != 0)
				{
					var newObject = newObjects[^1];
					stackedObjects = newObjects.Count > 1 ? newObjects.Take(newObjects.Count - 1).ToList() : null;
					SetStackedObjects(newObject, stackedObjects);
					__instance.stuff[x] = newObject;
				}
			}
			finally
			{
				IsDuringWobblyDestroy = false;
			}
		}
		else
		{
			var newObject = stackedObjects[^1];
			stackedObjects = stackedObjects.Count == 0 ? null : stackedObjects.Take(stackedObjects.Count - 1).ToList();
			SetStackedObjects(newObject, stackedObjects);
			SetWobbly(newObject, false);
			
			PushStackedObject(__instance, x, newObject);
		}
	}
}