using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Kokoro;

internal static class ShipPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnBeginTurn_Postfix_Last)), Priority.Last)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnAfterTurn)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnAfterTurn_Prefix_First)), Priority.First)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), "RenderStatusRow"),
			prefix: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_RenderStatusRow_Postfix))
		);
	}

	private static void Ship_OnBeginTurn_Postfix_Last(Ship __instance, State s, Combat c)
	{
		if (__instance != s.ship)
			return;

		Instance.MidrowScorchingManager.OnPlayerTurnStart(s, c);
		Instance.WormStatusManager.OnPlayerTurnStart(s, c);
	}

	private static void Ship_OnAfterTurn_Prefix_First(Ship __instance, State s)
	{
		Instance.OxidationStatusManager.OnTurnEnd(__instance, s);
	}

	private static void Ship_RenderStatusRow_Postfix(Ship __instance, G g)
	{
		Instance.OxidationStatusManager.ModifyStatusTooltips(__instance, g);
	}
}
