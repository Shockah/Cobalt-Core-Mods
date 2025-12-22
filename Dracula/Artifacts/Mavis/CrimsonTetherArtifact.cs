using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Shockah.Dracula;

internal sealed class CrimsonTetherArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("CrimsonTether", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.MavisDeck.Deck,
				pools = [ArtifactPool.EventOnly]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Mavis/CrimsonTether.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Mavis", "CrimsonTether", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Mavis", "CrimsonTether", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ASpawn), nameof(ASpawn.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ASpawn_Begin_Prefix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			.. StatusMeta.GetTooltips(ModEntry.Instance.MavisCharacter.MissingStatus.Status, 1),
			.. new MavisStuff().GetTooltips(),
			.. StatusMeta.GetTooltips(Status.droneShift, 1),
		];

	private static void AStatus_Begin_Prefix(AStatus __instance, State s, Combat c, out int? __state)
	{
		if (__instance.status != ModEntry.Instance.MavisCharacter.MissingStatus.Status)
		{
			__state = null;
			return;
		}
	
		var target = __instance.targetPlayer ? s.ship : c.otherShip;
		__state = target.Get(__instance.status);
	}
	
	private static void AStatus_Begin_Postfix(AStatus __instance, State s, Combat c, in int? __state)
	{
		if (__state is null)
			return;
	
		var target = __instance.targetPlayer ? s.ship : c.otherShip;
		var isZero = target.Get(__instance.status) == 0;
		var wasZero = __state.Value == 0;
		if (isZero == wasZero)
			return;
	
		if (isZero)
		{
			foreach (var kvp in c.stuff.ToList())
			{
				if (kvp.Value is not MavisStuff)
					continue;
				c.DestroyDroneAt(s, kvp.Key, target.isPlayerShip);
			}
		}
		else
		{
			c.QueueImmediate(new ASpawn { fromPlayer = target.isPlayerShip, thing = new MavisStuff() });
		}
	}

	private static void ASpawn_Begin_Prefix(ASpawn __instance, G g, State s, Combat c)
	{
		if (__instance.thing is not MavisStuff)
			return;
		if (__instance.fromPlayer && g.state.ship.GetPartTypeCount(PType.missiles) > 1 && !__instance.multiBayVolley)
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is CrimsonTetherArtifact) is not { } artifact)
			return;
		
		c.QueueImmediate(new AStatus { targetPlayer = __instance.fromPlayer, status = Status.droneShift, statusAmount = 1, artifactPulse = artifact.Key() });
	}
}