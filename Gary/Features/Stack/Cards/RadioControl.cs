using System.Reflection;
using HarmonyLib;

namespace Shockah.Gary;

internal partial class Stack
{
	private static void HandleRadioControl()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ADroneTurn), nameof(ADroneTurn.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ADroneTurn_GetTooltips_Postfix))
		);
	}

	private static void ADroneTurn_GetTooltips_Postfix(State s)
	{
		if (s.route is not Combat combat)
			return;
		
		ApplyToAllStackedObjects(combat, @object =>
		{
			if (@object.GetActions(s, combat) is not null)
				@object.hilight = 2;
		});
	}
}