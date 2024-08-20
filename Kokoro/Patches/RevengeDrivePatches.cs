using HarmonyLib;
using Nickel;

namespace Shockah.Kokoro;

// ReSharper disable InconsistentNaming
internal static class RevengeDrivePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RevengeDrive), nameof(RevengeDrive.OnPlayerLoseHull)),
			prefix: new HarmonyMethod(typeof(RevengeDrivePatches), nameof(RevengeDrive_OnPlayerLoseHull_Prefix)),
			postfix: new HarmonyMethod(typeof(RevengeDrivePatches), nameof(RevengeDrive_OnPlayerLoseHull_Postfix))
		);
	}

	private static void RevengeDrive_OnPlayerLoseHull_Prefix(RevengeDrive __instance, out bool __state)
		=> __state = __instance.alreadyActivated;

	private static void RevengeDrive_OnPlayerLoseHull_Postfix(RevengeDrive __instance, State state, Combat combat, ref bool __state)
	{
		if (!__instance.alreadyActivated || __state)
			return;

		// TODO: fix behavior for wrapped actions - this code won't trigger on these, but the original that we're fixing won't either
		if (!Instance.StatusLogicManager.IsAffectedByBoost(state, combat, state.ship, Status.overdrive))
			foreach (var action in combat.cardActions)
				if (action is AAttack { targetPlayer: false, fromDroneX: null } attack)
					attack.damage -= 1 + state.ship.Get(Status.boost);
	}
}
