using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Shockah.Shared;
using System.IO;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DizzyIsaacArtifact : DuoArtifact
{
	internal static ExternalSprite OxidationSprite { get; private set; } = null!;
	internal static ExternalStatus OxidationStatus { get; private set; } = null!;

	private static Ship? DestroyingShip;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnAfterTurn)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_OnAfterTurn_Prefix))
		);
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

	protected internal override void RegisterArt(ISpriteRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterArt(registry, namePrefix, definition);
		OxidationSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Icon.Oxidation",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Icons", "Oxidation.png"))
		);
	}

	protected internal override void RegisterStatuses(IStatusRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterStatuses(registry, namePrefix, definition);
		OxidationStatus = new(
			$"{namePrefix}.Oxidation",
			isGood: false,
			mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF00FFAD)),
			borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF98FFF7)),
			OxidationSprite,
			affectedByTimestop: false
		);
		OxidationStatus.AddLocalisation(I18n.OxidationStatusName, I18n.OxidationStatusDescription);
		registry.RegisterStatus(OxidationStatus);
	}

	private static void Ship_OnAfterTurn_Prefix(Ship __instance, State s, Combat c)
	{
		if (__instance.Get((Status)OxidationStatus.Id!.Value) < 7)
			return;

		__instance.Add(Status.corrode);
		__instance.Set((Status)OxidationStatus.Id!.Value, 0);
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
			status = (Status)OxidationStatus.Id!.Value,
			statusAmount = 1,
			targetPlayer = DestroyingShip.isPlayerShip
		});
	}
}