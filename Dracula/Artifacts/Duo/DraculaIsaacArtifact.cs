using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Dracula;

internal sealed class DraculaIsaacArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		var thisType = MethodBase.GetCurrentMethod()!.DeclaringType!;

		helper.Content.Artifacts.RegisterArtifact("DraculaIsaac", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/DraculaIsaac.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaIsaac", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "DraculaIsaac", "description"]).Localize
		});

		api.RegisterDuoArtifact(thisType, [ModEntry.Instance.DraculaDeck.Deck, Deck.goat]);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DestroyDroneAt)),
			prefix: new HarmonyMethod(thisType, nameof(Combat_DestroyDroneAt_Prefix)),
			postfix: new HarmonyMethod(thisType, nameof(Combat_DestroyDroneAt_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> new BatStuff().GetTooltips();

	private static void Combat_DestroyDroneAt_Prefix(Combat __instance, int x, ref StuffBase? __state)
		=> __state = __instance.stuff.TryGetValue(x, out var @object) ? @object : null;

	private static void Combat_DestroyDroneAt_Postfix(Combat __instance, State s, int x, ref StuffBase? __state)
	{
		if (__state?.targetPlayer != false)
			return;
		if (__state is not (AttackDrone or ShieldDrone or EnergyDrone))
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DraculaIsaacArtifact) is not { } artifact)
			return;

		artifact.Pulse();
		__instance.stuff[x] = new BatStuff();
	}
}