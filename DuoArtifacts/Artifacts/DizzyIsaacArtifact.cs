using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DizzyIsaacArtifact : DuoArtifact
{
	private static Ship? DestroyingShip;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(ASpawn_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(ASpawn_Begin_Finalizer))
		);
	}

	protected internal override void ApplyLatePatches(Harmony harmony)
	{
		base.ApplyLatePatches(harmony);
		harmony.TryPatchVirtual(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.DoDestroyedEffect)),
			postfix: new HarmonyMethod(GetType(), nameof(StuffBase_DoDestroyedEffect_Postfix))
		);
	}

	private static void AAttack_Begin_Prefix(AAttack __instance, State s, Combat c)
		=> DestroyingShip = __instance.targetPlayer ? c.otherShip : s.ship;

	private static void AAttack_Begin_Finalizer()
		=> DestroyingShip = null;

	private static void ASpawn_Begin_Prefix(ASpawn __instance, State s, Combat c)
		=> DestroyingShip = __instance.fromPlayer ? s.ship : c.otherShip;

	private static void ASpawn_Begin_Finalizer()
		=> DestroyingShip = null;

	private static void StuffBase_DoDestroyedEffect_Postfix(StuffBase __instance)
	{
		if (DestroyingShip is null)
			return;

		var artifact = StateExt.Instance?.EnumerateAllArtifacts().FirstOrDefault(a => a is DizzyIsaacArtifact);
		if (artifact is null)
			return;

		artifact.Pulse();
		(StateExt.Instance?.route as Combat)?.Queue(new AStatus
		{
			status = (Status)Instance.KokoroApi.OxidationStatus.Id!.Value,
			statusAmount = 1,
			targetPlayer = DestroyingShip.isPlayerShip
		});
	}
}