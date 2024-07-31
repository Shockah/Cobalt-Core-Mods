using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.Bloch;

internal sealed class VainMemoriesArtifact : Artifact, IRegisterable
{
	private static Card? LastCardPlayed = null;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		helper.Content.Artifacts.RegisterArtifact("VainMemories", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.BlochDeck.Deck,
				pools = ModEntry.GetArtifactPools(MethodBase.GetCurrentMethod()!.DeclaringType!)
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/VainMemories.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "VainMemories", "name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["artifact", "VainMemories", "description"]).Localize
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Finalizer))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToDiscard)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_SendCardToDiscard_Postfix))
		);
	}

	public override List<Tooltip>? GetExtraTooltips()
		=> StatusMeta.GetTooltips(Status.energyFragment, 1);

	private static void Combat_TryPlayCard_Prefix(Card card)
		=> LastCardPlayed = card;

	private static void Combat_TryPlayCard_Finalizer()
		=> LastCardPlayed = null;

	private static void Combat_SendCardToDiscard_Postfix(Combat __instance, State s, Card card)
	{
		if (!__instance.isPlayerTurn)
			return;
		if (card == LastCardPlayed)
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is VainMemoriesArtifact) is not { } artifact)
			return;

		__instance.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = Status.energyFragment,
			statusAmount = 1,
			artifactPulse = artifact.Key()
		});
	}
}