using HarmonyLib;
using Shockah.Shared;

namespace Shockah.CustomDifficulties;

internal static class RunConfigPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(RunConfig), nameof(RunConfig.SetDifficulty)),
			postfix: new HarmonyMethod(typeof(RunConfigPatches), nameof(RunConfig_SetDifficulty_Postfix))
		);
	}

	private static void RunConfig_SetDifficulty_Postfix(RunConfig __instance, int x)
		=> __instance.difficulty = x;
}
