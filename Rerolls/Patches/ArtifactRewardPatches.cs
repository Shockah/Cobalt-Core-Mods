using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Rerolls;

internal static class ArtifactRewardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static (int count, Deck? limitDeck, List<ArtifactPool>? limitPools, Rand? rngOverride)? LastGetOfferingArguments;
	private static readonly List<Artifact> RerolledArtifacts = new();

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.GetOffering)),
			postfix: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_GetOffering_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactReward), "GetBlockedArtifacts"),
			postfix: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_GetBlockedArtifacts_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.Render)),
			postfix: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_Render_Postfix))
		);
	}

	private static void Reroll(ArtifactReward menu, G g)
	{
		if (LastGetOfferingArguments is not { } arguments)
			return;

		var artifact = g.state.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		RerolledArtifacts.AddRange(menu.artifacts);
		menu.artifacts = ArtifactReward.GetOffering(g.state, arguments.count, arguments.limitDeck, arguments.limitPools, arguments.rngOverride);
		artifact.RerollsLeft--;
		artifact.Pulse();
	}

	private static void ArtifactReward_GetOffering_Postfix(int count, Deck? limitDeck, List<ArtifactPool>? limitPools, Rand? rngOverride)
		=> LastGetOfferingArguments = (count, limitDeck, limitPools, rngOverride);

	private static void ArtifactReward_GetBlockedArtifacts_Postfix(ref HashSet<Type> __result)
	{
		foreach (var artifact in RerolledArtifacts)
			__result.Add(artifact.GetType());
		RerolledArtifacts.Clear();
	}

	private static void ArtifactReward_Render_Postfix(ArtifactReward __instance, G g)
	{
		var artifact = g.state.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault();
		if (artifact is null || artifact.RerollsLeft <= 0)
			return;

		SharedArt.ButtonText(g, new Vec(210, 228), (UIKey)(UK)21370001, I18n.RerollButton, null, null, inactive: artifact.RerollsLeft <= 0, new MouseDownHandler(() => Reroll(__instance, g)), platformButtonHint: Btn.Y);
	}
}
