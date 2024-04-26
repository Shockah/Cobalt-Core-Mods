using HarmonyLib;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Rerolls;

internal sealed class ArtifactRerollManager
{
	private static AArtifactOffering? ActionContext;
	private static MapArtifact? MapNodeContext;

	public ArtifactRerollManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(AArtifactOffering), nameof(AArtifactOffering.BeginWithRoute)),
			prefix: new HarmonyMethod(GetType(), nameof(AArtifactOffering_BeginWithRoute_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AArtifactOffering_BeginWithRoute_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(MapArtifact), nameof(MapArtifact.MakeRoute)),
			prefix: new HarmonyMethod(GetType(), nameof(MapArtifact_MakeRoute_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(MapArtifact_MakeRoute_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.GetBlockedArtifacts)),
			postfix: new HarmonyMethod(GetType(), nameof(ArtifactReward_GetBlockedArtifacts_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.Render)),
			postfix: new HarmonyMethod(GetType(), nameof(ArtifactReward_Render_Postfix))
		);
	}

	private static void Reroll(G g, ArtifactReward route)
	{
		if (g.state.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault() is not { } artifact || artifact.RerollsLeft <= 0)
			return;

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<AArtifactOffering>(route, "OriginalAction") is { } originalAction)
		{
			var newAction = Mutil.DeepCopy(originalAction);

			var rerolledArtifacts = ModEntry.Instance.Helper.ModData.ObtainModData(newAction, "RerolledArtifacts", () => new HashSet<string>());
			foreach (var artifactChoice in route.artifacts)
				rerolledArtifacts.Add(artifactChoice.Key());

			g.state.GetCurrentQueue().QueueImmediate(newAction);
			g.CloseRoute(route);
		}
		else if (ModEntry.Instance.Helper.ModData.GetOptionalModData<MapArtifact>(route, "OriginalMapNode") is { } originalMapNode)
		{
			var newMapNode = Mutil.DeepCopy(originalMapNode);

			var rerolledArtifacts = ModEntry.Instance.Helper.ModData.ObtainModData(newMapNode, "RerolledArtifacts", () => new HashSet<string>());
			foreach (var artifactChoice in route.artifacts)
				rerolledArtifacts.Add(artifactChoice.Key());

			g.state.ChangeRoute(() => newMapNode.MakeRoute(g.state));
		}
		else
		{
			return;
		}

		artifact.RerollsLeft--;
		artifact.Pulse();
	}

	private static void AArtifactOffering_BeginWithRoute_Prefix(AArtifactOffering __instance)
		=> ActionContext = __instance;

	private static void AArtifactOffering_BeginWithRoute_Finalizer(AArtifactOffering __instance, State s, Combat c, Route? __result)
	{
		ActionContext = null;

		if (s.route == c)
			return;
		if (__result is not ArtifactReward route)
			return;

		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "OriginalAction", __instance);
	}

	private static void MapArtifact_MakeRoute_Prefix(MapArtifact __instance)
		=> MapNodeContext = __instance;

	private static void MapArtifact_MakeRoute_Finalizer(MapArtifact __instance, Route? __result)
	{
		MapNodeContext = null;

		if (__result is not ArtifactReward route)
			return;

		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "OriginalMapNode", __instance);
	}

	private static void ArtifactReward_GetBlockedArtifacts_Postfix(HashSet<Type> __result)
	{
		HashSet<string> rerolledArtifacts;

		if (ActionContext is { } action)
		{
			if (ModEntry.Instance.Helper.ModData.GetOptionalModData<HashSet<string>>(action, "RerolledArtifacts") is not { } modDataRerolledArtifacts)
				return;
			rerolledArtifacts = modDataRerolledArtifacts;
		}
		else if (MapNodeContext is { } mapNode)
		{
			if (ModEntry.Instance.Helper.ModData.GetOptionalModData<HashSet<string>>(mapNode, "RerolledArtifacts") is not { } modDataRerolledArtifacts)
				return;
			rerolledArtifacts = modDataRerolledArtifacts;
		}
		else
		{
			return;
		}

		foreach (var rerolledArtifact in rerolledArtifacts)
			if (DB.artifacts.TryGetValue(rerolledArtifact, out var artifactType))
				__result.Add(artifactType);
	}

	private static void ArtifactReward_Render_Postfix(ArtifactReward __instance, G g)
	{
		if (g.state.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault() is not { } artifact || artifact.RerollsLeft <= 0)
			return;
		if (!ModEntry.Instance.Helper.ModData.ContainsModData(__instance, "OriginalAction") && !ModEntry.Instance.Helper.ModData.ContainsModData(__instance, "OriginalMapNode"))
			return;

		SharedArt.ButtonText(
			g,
			new Vec(210, 228),
			(UIKey)(UK)21370001,
			ModEntry.Instance.Localizations.Localize(["button"]),
			inactive: artifact.RerollsLeft <= 0,
			onMouseDown: new MouseDownHandler(() => Reroll(g, __instance)),
			platformButtonHint: Btn.Y
		);
		if (g.boxes.FirstOrDefault(b => b.key == new UIKey((UK)21370001)) is { } box)
			box.onInputPhase = new InputPhaseHandler(() =>
			{
				if (Input.GetGpDown(Btn.Y))
					Reroll(g, __instance);
			});
	}
}
