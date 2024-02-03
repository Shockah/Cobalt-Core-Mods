using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Rerolls;

internal static class ArtifactRewardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly List<Artifact> RerolledArtifacts = [];

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
		var artifact = g.state.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault();
		if (artifact is null || artifact.LastArtifactOfferingConfig is not { } config)
			return;

		RerolledArtifacts.AddRange(menu.artifacts);
		menu.artifacts = ArtifactReward.GetOffering(g.state, config.Count, config.LimitDeck, config.LimitPools);
		artifact.RerollsLeft--;
		artifact.Pulse();
	}

	private static void ArtifactReward_GetOffering_Postfix(State s, int count, Deck? limitDeck, List<ArtifactPool>? limitPools)
	{
		var artifact = s.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault();
		if (artifact is null)
			return;
		artifact.LastArtifactOfferingConfig = new(count, limitDeck, limitPools);
	}

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

		SharedArt.ButtonText(
			g,
			new Vec(210, 228),
			(UIKey)(UK)21370001,
			I18n.RerollButton,
			inactive: artifact.RerollsLeft <= 0,
			onMouseDown: new MouseDownHandler(() => Reroll(__instance, g)),
			platformButtonHint: Btn.Y
		);
		if (g.boxes.FirstOrDefault(b => b.key == new UIKey((UK)21370001)) is { } box)
			box.onInputPhase = new InputPhaseHandler(() =>
			{
				if (Input.GetGpDown(Btn.Y))
					Reroll(__instance, g);
			});
	}
}
