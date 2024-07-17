using HarmonyLib;

namespace Shockah.Dracula;

internal sealed class NegativeOverdriveManager
{
	public NegativeOverdriveManager()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.CanBeNegative)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_CanBeNegative_Postfix))
		);
	}

	private static void Ship_CanBeNegative_Postfix(Status status, ref bool __result)
	{
		if (status == Status.overdrive)
			__result = true;
	}
}
