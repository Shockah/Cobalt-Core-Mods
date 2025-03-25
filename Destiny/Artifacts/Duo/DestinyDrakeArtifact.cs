using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Destiny;

internal sealed class DestinyDrakeArtifact : Artifact, IRegisterable, IDestinyApi.IHook
{
	private static bool DuringOverheat;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		if (ModEntry.Instance.DuoArtifactsApi is not { } api)
			return;
		
		helper.Content.Artifacts.RegisterArtifact("DestinyDrake", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = api.DuoArtifactVanillaDeck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Artifacts/Duo/Drake.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Drake", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "Duo", "Drake", "description"]).Localize
		});

		api.RegisterDuoArtifact(MethodBase.GetCurrentMethod()!.DeclaringType!, [ModEntry.Instance.DestinyDeck.Deck, Deck.eunice]);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AOverheat), nameof(AOverheat.Begin)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AOverheat_Begin_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AOverheat_Begin_Finalizer))
		);
	}

	public override List<Tooltip> GetExtraTooltips()
		=> [
			new TTGlossary("action.overheat"),
			.. StatusMeta.GetTooltips(PristineShield.PristineShieldStatus.Status, 1),
		];

	public void OnPristineShieldTrigger(IDestinyApi.IHook.IOnPristineShieldTriggerArgs args)
	{
		if (!DuringOverheat)
			return;
		if (!args.Ship.isPlayerShip)
			return;
		if (args.State.EnumerateAllArtifacts().FirstOrDefault(a => a is DestinyDrakeArtifact) is not { } artifact)
			return;

		args.TickDown = false;
		artifact.Pulse();
	}

	private static void AOverheat_Begin_Prefix()
		=> DuringOverheat = true;

	private static void AOverheat_Begin_Finalizer()
		=> DuringOverheat = false;
}