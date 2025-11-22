using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Shockah.Gary;

internal partial class Stack
{
	private static void HandleCatch()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASlurpMidrowObject), nameof(ASlurpMidrowObject.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASlurpMidrowObject_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASlurpMidrowObject_Begin_Finalizer))
		);
	}

	private static void ASlurpMidrowObject_Begin_Prefix(Combat c, out Dictionary<int, List<StuffBase>> __state)
	{
		__state = [];
		foreach (var kvp in c.stuff)
		{
			if (GetStackedObjects(kvp.Value) is not { } stackedObjects)
				continue;
			__state[kvp.Key] = stackedObjects;
			SetStackedObjects(kvp.Value, null);
		}
	}

	private static void ASlurpMidrowObject_Begin_Finalizer(Combat c, in Dictionary<int, List<StuffBase>> __state)
	{
		foreach (var kvp in __state)
		{
			if (c.stuff.TryGetValue(kvp.Key, out var @object))
			{
				SetStackedObjects(@object, kvp.Value);
			}
			else
			{
				foreach (var stackedObject in kvp.Value)
					PushStackedObject(c, kvp.Key, stackedObject);
			}
		}
	}
}