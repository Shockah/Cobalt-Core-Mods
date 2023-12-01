using HarmonyLib;
using Shockah.Shared;
using System.Linq;

namespace Shockah.Rerolls.Patches;

internal class MapExitPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(MapExit), nameof(MapExit.MakeRoute)),
			postfix: new HarmonyMethod(typeof(MapExitPatches), nameof(MapExit_MakeRoute_Postfix))
		);
	}

	private static void MapExit_MakeRoute_Postfix(State s)
	{
		var artifact = s.EnumerateAllArtifacts().OfType<RerollArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		artifact.RerollsLeft++;
		artifact.Pulse();
	}
}
