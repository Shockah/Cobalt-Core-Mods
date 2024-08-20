using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Soggins;

internal static class ShipPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.ResetAfterCombat)),
			postfix: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_ResetAfterCombat_Postfix))
		);
	}

	private static void Ship_ResetAfterCombat_Postfix(Ship __instance)
	{
		if (MG.inst.g.state is not { } state)
			return;
		if (state.ship != __instance)
			return;

		Instance.Api.SetSmugEnabled(state, __instance, false);
	}
}
