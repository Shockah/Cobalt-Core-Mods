using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
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

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DestroyDroneAt)),
			prefix: new HarmonyMethod(thisType, nameof(Combat_DestroyDroneAt_Prefix)),
			postfix: new HarmonyMethod(thisType, nameof(Combat_DestroyDroneAt_Postfix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> new BatStuff().GetTooltips();

	private static void Combat_DestroyDroneAt_Prefix(Combat __instance, int x, out StuffBase? __state)
		=> __state = __instance.stuff.GetValueOrDefault(x);

	private static void Combat_DestroyDroneAt_Postfix(Combat __instance, State s, int x, in StuffBase? __state)
	{
		if (__state is not (AttackDrone or ShieldDrone or EnergyDrone))
			return;
		if (__state.IsHostile() && !__state.IsFriendly())
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is DraculaIsaacArtifact) is not { } artifact)
			return;

		artifact.Pulse();
		__instance.stuff[x] = new BatStuff { x = x, xLerped = x };
	}
}