using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaIsaacArtifact : Artifact, IDraculaArtifact
{
	private static Ship? DestroyingShip;

	public static void Register(IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			throw new InvalidOperationException();
		var thisType = MethodBase.GetCurrentMethod()!.DeclaringType!;

		helper.Content.Artifacts.RegisterArtifact("DraculaIsaac", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaIsaac.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaIsaac", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaIsaac", "description"]).Localize
		});

		api.RegisterDuoArtifact(thisType, [ModEntry.Instance.DraculaDeck.Deck, Deck.goat]);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(thisType, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(thisType, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(thisType, nameof(ASpawn_Begin_Prefix)),
			finalizer: new HarmonyMethod(thisType, nameof(ASpawn_Begin_Finalizer))
		);

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;

			ModEntry.Instance.Harmony.TryPatchVirtual(
				logger: ModEntry.Instance.Logger,
				original: () => AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.DoDestroyedEffect)),
				postfix: new HarmonyMethod(thisType, nameof(StuffBase_DoDestroyedEffect_Postfix))
			);
		};
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips(ModEntry.Instance.BloodMirrorStatus.Status, 1);

	private static void AAttack_Begin_Prefix(AAttack __instance, State s, Combat c)
		=> DestroyingShip = __instance.targetPlayer ? c.otherShip : s.ship;

	private static void AAttack_Begin_Finalizer()
		=> DestroyingShip = null;

	private static void ASpawn_Begin_Prefix(ASpawn __instance, State s, Combat c)
		=> DestroyingShip = __instance.fromPlayer ? s.ship : c.otherShip;

	private static void ASpawn_Begin_Finalizer()
		=> DestroyingShip = null;

	private static void StuffBase_DoDestroyedEffect_Postfix()
	{
		if (DestroyingShip is null)
			return;

		var artifact = MG.inst.g.state.EnumerateAllArtifacts().FirstOrDefault(a => a is DraculaIsaacArtifact);
		if (artifact is null)
			return;

		var action = new AHurt
		{
			targetPlayer = DestroyingShip.isPlayerShip,
			hurtAmount = 2,
			artifactPulse = artifact.Key()
		};
		action.SetBloodMirrorDepth(1);
		(MG.inst.g.state.route as Combat)?.Queue(action);
	}
}