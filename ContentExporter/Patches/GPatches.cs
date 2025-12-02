using HarmonyLib;
using Shockah.Shared;

namespace Shockah.ContentExporter;

internal sealed class GPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(G), nameof(G.Render)),
			prefix: new HarmonyMethod(typeof(GPatches), nameof(G_Render_Prefix))
		);
	}

	private static void G_Render_Prefix(G __instance)
	{
		for (var i = 0; i < 2; i++)
			Instance.RunNextTask(__instance);
	}
}