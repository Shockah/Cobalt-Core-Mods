using System.Linq;
using HarmonyLib;
using Nickel;

namespace Shockah.DuoArtifacts;

internal sealed class DizzyDrakeArtifact : DuoArtifact
{
	private const int ShieldInsteadOfHull = 2;

	private static bool DuringOverheat;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AOverheat), nameof(AOverheat.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AOverheat_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AOverheat_Begin_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Prefix))
		);
	}

	private static void AOverheat_Begin_Prefix()
		=> DuringOverheat = true;

	private static void AOverheat_Begin_Finalizer()
		=> DuringOverheat = false;

	private static bool Ship_DirectHullDamage_Prefix(Ship __instance, State s, Combat c)
	{
		if (!DuringOverheat)
			return true;
		if (!__instance.isPlayerShip)
			return true;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DizzyDrakeArtifact) is not { } artifact)
			return true;
		
		var totalShield = __instance.Get(Status.shield) + __instance.Get(Status.tempShield);
		if (s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			totalShield += __instance.Get(Status.shard);

		if (totalShield < ShieldInsteadOfHull)
			return true;

		DuringOverheat = false;
		artifact.Pulse();
		__instance.NormalDamage(s, c, ShieldInsteadOfHull, null);
		return false;
	}
}
