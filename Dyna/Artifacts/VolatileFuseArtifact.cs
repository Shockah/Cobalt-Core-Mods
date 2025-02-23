using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Dyna;

internal sealed class VolatileFuseArtifact : Artifact, IRegisterable, IDynaHook
{
	private const int Threshold = 8;
	
	public int Counter;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("VolatileFuse", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.DynaDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/VolatileFuse.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "VolatileFuse", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "VolatileFuse", "description"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AAttack_Begin_Prefix_First)), priority: Priority.First)
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> new BlastwaveManager.BlastwaveAction { Source = new(), Damage = 1, WorldX = 0 }.GetTooltips(MG.inst.g?.state ?? DB.fakeState);

	public override int? GetDisplayNumber(State s)
		=> Counter;

	private static void AAttack_Begin_Prefix_First(AAttack __instance, State s)
	{
		if (__instance.IsBlastwave())
			return;
		if (s.EnumerateAllArtifacts().OfType<VolatileFuseArtifact>().FirstOrDefault() is not { } artifact)
			return;
		
		artifact.Counter++;
		if (artifact.Counter < Threshold)
			return;

		artifact.Counter -= Threshold;
		artifact.Pulse();
		__instance.SetBlastwave(1);
	}
}