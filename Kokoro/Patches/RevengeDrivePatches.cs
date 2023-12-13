using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Kokoro;

internal static class RevengeDrivePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(RevengeDrive), nameof(RevengeDrive.OnPlayerLoseHull)),
			prefix: new HarmonyMethod(typeof(RevengeDrivePatches), nameof(RevengeDrive_OnPlayerLoseHull_Prefix)),
			postfix: new HarmonyMethod(typeof(RevengeDrivePatches), nameof(RevengeDrive_OnPlayerLoseHull_Postfix))
		);
	}

	private static void RevengeDrive_OnPlayerLoseHull_Prefix(RevengeDrive __instance, ref bool __state)
		=> __state = __instance.alreadyActivated;

	private static void RevengeDrive_OnPlayerLoseHull_Postfix(RevengeDrive __instance, State state, Combat combat, ref bool __state)
	{
		if (!__instance.alreadyActivated || __state)
			return;

		if (!Instance.StatusLogicManager.IsAffectedByBoost(state, combat, state.ship, Status.overdrive))
			foreach (var action in combat.cardActions)
				if (action is AAttack attack && !attack.targetPlayer && attack.fromDroneX is null)
					attack.damage -= 1 + state.ship.Get(Status.boost);
	}
}
