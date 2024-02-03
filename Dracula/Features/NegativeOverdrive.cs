using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Dracula;

internal sealed class NegativeOverdriveManager
{
	public NegativeOverdriveManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.CanBeNegative)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_CanBeNegative_Postfix))
		);
	}

	private static void Ship_CanBeNegative_Postfix(Status status, ref bool __result)
	{
		if (status == Status.overdrive)
			__result = true;
	}
}
