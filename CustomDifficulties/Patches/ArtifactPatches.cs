using HarmonyLib;
using Shockah.Shared;

namespace Shockah.CustomDifficulties;

internal static class ArtifactPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.GetLocName)),
			postfix: new HarmonyMethod(typeof(ArtifactPatches), nameof(Artifact_GetLocName_Postfix))
		);
	}

	private static void Artifact_GetLocName_Postfix(Artifact __instance, ref string __result)
	{
		if (__instance is not HARDMODE hardmode || hardmode.difficulty != ModEntry.EasyDifficultyLevel)
			return;
		__result = I18n.EasyModeDifficultyName;
	}
}
