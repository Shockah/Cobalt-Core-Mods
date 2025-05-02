using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dyna;

internal sealed class DynaIsaacArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;

		helper.Content.Artifacts.RegisterArtifact("DynaIsaac", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DynaIsaac.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaIsaac", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DynaIsaac", "description"]).Localize,
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DynaDeck.Deck, Deck.goat]);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. new AttackDrone().GetTooltips(),
			.. new BlastwaveManager.BlastwaveAction { Source = new(), LocalX = 0, Damage = 0, Range = 1 }.GetTooltips(DB.fakeState),
			.. StatusMeta.GetTooltips(NitroManager.NitroStatus.Status, Math.Max(MG.inst.g.state.ship.Get(NitroManager.NitroStatus.Status), 1)),
			.. StatusMeta.GetTooltips(NitroManager.TempNitroStatus.Status, Math.Max(MG.inst.g.state.ship.Get(NitroManager.TempNitroStatus.Status), 1)),
		];

	private static void AAttack_Begin_Prefix(AAttack __instance, State s, Combat c)
	{
		if (__instance.targetPlayer)
			return;
		if (__instance.fromDroneX is not { } droneX)
			return;
		if (!c.stuff.TryGetValue(droneX, out var @object))
			return;
		if (@object is not AttackDrone)
			return;
		if (__instance.IsBlastwave())
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DynaIsaacArtifact) is not { } artifact)
			return;

		artifact.Pulse();
		__instance.SetBlastwave(damage: ModEntry.Instance.Api.GetBlastwaveDamage(card: null, s, 0, targetPlayer: __instance.targetPlayer));
	}
}