using HarmonyLib;
using Shockah.Shared;
using System.Reflection;

namespace Shockah.Kokoro;

internal static class ACardOfferingPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ACardOffering), nameof(ACardOffering.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardOffering_BeginWithRoute_Postfix))
		);
	}

	private static void ACardOffering_BeginWithRoute_Postfix(ACardOffering __instance, Route? __result)
	{
		if (__result is not CardReward route)
			return;

		if (Instance.Api.TryGetExtensionData(__instance, "destination", out CardDestination destination))
			Instance.Api.SetExtensionData(route, "destination", destination);
		if (Instance.Api.TryGetExtensionData(__instance, "destinationInsertRandomly", out bool destinationInsertRandomly))
			Instance.Api.SetExtensionData(route, "destinationInsertRandomly", destinationInsertRandomly);
	}
}