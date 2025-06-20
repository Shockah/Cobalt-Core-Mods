using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaDrakeArtifact : Artifact, IRegisterable
{
	private static AAttack? CurrentAttack;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("DraculaDrake", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaDrake.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaDrake", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaDrake", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DraculaDeck.Deck, Deck.eunice]);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("action.attackPiercing"),
			new TTGlossary("action.stun"),
			.. StatusMeta.GetTooltips(ModEntry.Instance.BleedingStatus.Status, 1),
		];

	public override void OnEnemyGetHit(State state, Combat combat, Part? part)
	{
		base.OnEnemyGetHit(state, combat, part);
		if (CurrentAttack is null)
			return;
		if (CurrentAttack is { piercing: false, stunEnemy: false })
			return;

		combat.QueueImmediate(new AStatus
		{
			targetPlayer = CurrentAttack.targetPlayer,
			status = ModEntry.Instance.BleedingStatus.Status,
			statusAmount = 1,
			artifactPulse = Key()
		});
	}

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> CurrentAttack = __instance;

	private static void AAttack_Begin_Finalizer()
		=> CurrentAttack = null;
}