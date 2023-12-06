using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Wormholes;

internal static class MapRoutePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(MapRoute), nameof(MapRoute.OnClickDestination)),
			prefix: new HarmonyMethod(typeof(MapRoutePatches), nameof(MapRoute_OnClickDestination_Prefix))
		);
	}

	private static bool MapRoute_OnClickDestination_Prefix(G g, Vec key, bool force)
	{
		if (force)
			return true;
		if (!g.state.map.markers.TryGetValue(key, out var marker) || marker.contents is null)
			return true;
		return !marker.wasVisited && !marker.wasCleared;
	}
}
