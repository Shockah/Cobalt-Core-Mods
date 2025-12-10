using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class BreadnaughtMinigunArtifact : Artifact, IRegisterable
{
	[JsonProperty]
	private int LostEnergy;
	
	[JsonProperty]
	private int GainedSpin;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("BreadnaughtMinigun", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = Deck.colorless,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Breadnaught/Artifact/Minigun.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "artifact", "Minigun", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["ship", "Breadnaught", "artifact", "Minigun", "description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_Update_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.BubbleEvents)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_BubbleEvents_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_BubbleEvents_Postfix))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> StatusMeta.GetTooltips(BreadnaughtBarrelSpin.BarrelSpinStatus.Status, 1);

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		LostEnergy = 0;
		GainedSpin = 0;
	}

	public override void OnQueueEmptyDuringPlayerTurn(State state, Combat combat)
	{
		base.OnQueueEmptyDuringPlayerTurn(state, combat);
		if (LostEnergy <= GainedSpin)
			return;

		var amount = LostEnergy - GainedSpin;
		GainedSpin = LostEnergy;
		combat.QueueImmediate(new AStatus { targetPlayer = true, status = BreadnaughtBarrelSpin.BarrelSpinStatus.Status, statusAmount = amount, artifactPulse = Key(), timer = 0 });
	}

	private static void Combat_Update_Prefix(Combat __instance, out int __state)
		=> __state = __instance.energy;

	private static void Combat_Update_Postfix(Combat __instance, G g, in int __state)
	{
		if (!__instance.isPlayerTurn)
			return;
		
		var energyLost = __state - __instance.energy;
		if (energyLost <= 0)
			return;
		
		if (g.state.EnumerateAllArtifacts().OfType<BreadnaughtMinigunArtifact>().FirstOrDefault() is not { } artifact)
			return;
		
		artifact.LostEnergy += energyLost;
	}

	private static void G_BubbleEvents_Prefix(G __instance, out int __state)
		=> __state = (__instance.state?.route as Combat)?.energy ?? 0;

	private static void G_BubbleEvents_Postfix(G __instance, in int __state)
	{
		if (__instance.state?.route is not Combat combat)
			return;
		if (!combat.isPlayerTurn)
			return;
		
		var energyLost = __state - combat.energy;
		if (energyLost <= 0)
			return;
		
		if (__instance.state.EnumerateAllArtifacts().OfType<BreadnaughtMinigunArtifact>().FirstOrDefault() is not { } artifact)
			return;
		
		artifact.LostEnergy += energyLost;
	}
}