using HarmonyLib;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Kokoro;

// ReSharper disable InconsistentNaming
internal static class StatusMetaPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StatusMeta), nameof(StatusMeta.GetTooltips)),
			postfix: new HarmonyMethod(typeof(StatusMetaPatches), nameof(StatusMeta_GetTooltips_Postfix))
		);
	}

	private static void StatusMeta_GetTooltips_Postfix(Status status, int amt, ref List<Tooltip> __result)
		=> __result = Instance.StatusRenderManager.OverrideStatusTooltips(status, amt, __result);
}
