using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Shockah.Gary;

internal partial class Stack
{
	private static void HandleMedusaField()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMedusaField), nameof(AMedusaField.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMedusaField_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMedusaField_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMedusaField), nameof(AMedusaField.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMedusaField_GetTooltips_Postfix))
		);
	}
	
	private static void AMedusaField_Begin_Prefix(Combat c, out Dictionary<int, List<StuffBase>> __state)
	{
		__state = [];
		foreach (var kvp in c.stuff)
			if (GetStackedObjects(kvp.Value) is { } stackedObjects)
				__state[kvp.Key] = stackedObjects;
	}

	private static void AMedusaField_Begin_Postfix(Combat c, in Dictionary<int, List<StuffBase>> __state)
	{
		foreach (var kvp in __state)
		{
			var stackedObjects = kvp.Value.Select(StuffBase (stackedObject) => new Geode
			{
				x = stackedObject.x,
				xLerped = stackedObject.xLerped,
				bubbleShield = stackedObject.bubbleShield,
				targetPlayer = stackedObject.targetPlayer,
				age = stackedObject.age,
			}).ToList();

			if (c.stuff.TryGetValue(kvp.Key, out var @object))
			{
				SetStackedObjects(@object, stackedObjects);
			}
			else
			{
				foreach (var stackedObject in stackedObjects)
					PushStackedObject(c, kvp.Key, stackedObject);
			}
		}
	}

	private static void AMedusaField_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllStackedObjects(combat, @object =>
		{
			@object.hilight = 2;
		});
	}
}