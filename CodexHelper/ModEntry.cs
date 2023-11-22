using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.CodexHelper;

public sealed class ModEntry : IModManifest
{
	public string Name { get; init; } = typeof(ModEntry).FullName!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	private static bool IsRenderingCardRewardMenu = false;

	public void BootMod(IModLoaderContact contact)
	{
		Harmony harmony = new(Name);
		harmony.TryPatch(
			logger: Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.Render)),
			prefix: new HarmonyMethod(GetType(), nameof(CardReward_Render_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(CardReward_Render_Finalizer))
		);
		harmony.TryPatch(
			logger: Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			postfix: new HarmonyMethod(GetType(), nameof(Card_Render_Postfix))
		);
		harmony.TryPatch(
			logger: Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.Render)),
			postfix: new HarmonyMethod(GetType(), nameof(ArtifactReward_Render_Postfix))
		);
	}

	private static void CardReward_Render_Prefix()
		=> IsRenderingCardRewardMenu = true;

	private static void CardReward_Render_Finalizer()
		=> IsRenderingCardRewardMenu = false;

	private static void Card_Render_Postfix(Card __instance, G g)
	{
		if (!IsRenderingCardRewardMenu)
			return;
		if (g.state.storyVars.cardsOwned.Contains(__instance.Key()))
			return;

		var rect = __instance.GetScreenRect();
		Draw.Text(I18n.MissingFromCodex, __instance.targetPos.x + rect.w / 2 - 2, __instance.targetPos.y - 8, DB.pinch, Colors.textMain, align: TAlign.Center, extraScale: 0.75);
	}

	private static void ArtifactReward_Render_Postfix(ArtifactReward __instance, G g)
	{
		foreach (var artifact in __instance.artifacts)
		{
			if (g.state.storyVars.artifactsOwned.Contains(artifact.Key()))
				continue;

			Draw.Text(I18n.MissingFromCodex, artifact.lastScreenPos.x + 170, artifact.lastScreenPos.y + 12, DB.pinch, Colors.textMain, align: TAlign.Right, extraScale: 0.75);
		}
	}
}
