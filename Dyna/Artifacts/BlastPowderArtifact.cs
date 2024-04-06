using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class BlastPowderArtifact : Artifact, IRegisterable
{
	private static AAttack? AttackContext;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("BlastPowder", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/BlastPowder.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BlastPowder", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "BlastPowder", "description"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_NormalDamage_Prefix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> new BlastwaveManager.BlastwaveAction { Source = new(), Damage = 1, WorldX = 0 }.GetTooltips(DB.fakeState);

	private static void AAttack_Begin_Prefix(AAttack __instance)
		=> AttackContext = __instance;

	private static void AAttack_Begin_Finalizer()
		=> AttackContext = null;

	private static void Ship_NormalDamage_Prefix(Ship __instance, State s, ref int incomingDamage, int? maybeWorldGridX)
	{
		if (AttackContext is not { } attack || maybeWorldGridX is not { } worldX)
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is BlastPowderArtifact) is not { } artifact)
			return;
		if (attack.IsBlastwave())
			return;
		if (__instance.GetPartAtWorldX(worldX) is not { } part || part.type == PType.empty)
			return;
		if (part.GetStickedCharge() is null)
			return;

		incomingDamage++;
		artifact.Pulse();
	}
}