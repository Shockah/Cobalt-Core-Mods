using System.Reflection;
using HarmonyLib;
using Nickel;

namespace Shockah.DuoArtifacts;

internal sealed class StatePatches
{
	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.PopulateRun)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Postfix))
		);
	}
	
	private static void State_PopulateRun_Postfix(State __instance)
		=> ModEntry.Instance.Helper.ModData.RemoveModData(__instance, "DuosSeenThisRun");
}