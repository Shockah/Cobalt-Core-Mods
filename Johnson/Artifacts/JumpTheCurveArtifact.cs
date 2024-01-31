using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Linq;
using System.Reflection;

namespace Shockah.Johnson;

internal sealed class JumpTheCurveArtifact : Artifact, IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("JumpTheCurve", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.JohnsonDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/JumpTheCurve.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "JumpTheCurve", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "JumpTheCurve", "description"]).Localize
		});

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(State), nameof(State.SendCardToDeck)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_SendCardToDeck_Postfix))
		);
	}

	private static void State_SendCardToDeck_Postfix(State __instance, Card card)
	{
		if (__instance.route is Combat combat && !combat.EitherShipIsDead(__instance))
			return;

		var artifact = __instance.EnumerateAllArtifacts().FirstOrDefault(a => a is JumpTheCurveArtifact);
		if (artifact is null)
			return;

		card.discount--;
		artifact.Pulse();
		if (card.IsUpgradable())
			__instance.GetCurrentQueue().QueueImmediate(new ATemporarilyUpgrade
			{
				CardId = card.uuid,
				artifactPulse = artifact.Key()
			});
	}
}
