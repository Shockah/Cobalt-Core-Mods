using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Bloch;

internal sealed class NegativeBoostManager
{
	public NegativeBoostManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.CanBeNegative)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_CanBeNegative_Postfix))
		);
	}

	private static void Ship_CanBeNegative_Postfix(Status status, ref bool __result)
	{
		if (status == Status.boost)
			__result = true;
	}
}