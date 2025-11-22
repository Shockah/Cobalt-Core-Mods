using System.Reflection;
using HarmonyLib;

namespace Shockah.Gary;

internal partial class Stack
{
	private static void HandleBubbleField()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ABubbleField), nameof(ABubbleField.Begin)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ABubbleField_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ABubbleField), nameof(ABubbleField.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ABubbleField_GetTooltips_Postfix))
		);
	}

	private static void ABubbleField_Begin_Postfix(Combat c)
	{
		ApplyToAllStackedObjects(c, @object =>
		{
			@object.bubbleShield = true;
		});
	}

	private static void ABubbleField_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllStackedObjects(combat, @object =>
		{
			@object.hilight = 2;
		});
	}
}