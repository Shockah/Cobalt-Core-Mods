using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

internal static class StatusMetaPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(StatusMeta), nameof(StatusMeta.GetTooltips)),
			postfix: new HarmonyMethod(typeof(StatusMetaPatches), nameof(StatusMeta_GetTooltips_Postfix))
		);
	}

	private static void StatusMeta_GetTooltips_Postfix(Status status, int amt, ref List<Tooltip> __result)
	{
		foreach (var hook in Instance.StatusRenderManager)
			__result = hook.OverrideStatusTooltips(status, amt, __result);
	}
}
