using HarmonyLib;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Kokoro;

// ReSharper disable InconsistentNaming
internal static class StuffBasePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.DrawWithHilight)),
			postfix: new HarmonyMethod(typeof(StuffBasePatches), nameof(StuffBase_DrawWithHilight_Postfix))
		);
	}

	public static void ApplyLate(IHarmony harmony)
	{
		harmony.PatchVirtual(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetTooltips)),
			postfix: new HarmonyMethod(typeof(StuffBasePatches), nameof(StuffBase_GetTooltips_Postfix))
		);
		harmony.PatchVirtual(
			original: AccessTools.DeclaredMethod(typeof(StuffBase), nameof(StuffBase.GetActionsOnDestroyed)),
			postfix: new HarmonyMethod(typeof(StuffBasePatches), nameof(StuffBase_GetActionsOnDestroyed_Postfix))
		);
	}

	private static void StuffBase_DrawWithHilight_Postfix(StuffBase __instance, G g, Spr id, Vec v, bool flipX, bool flipY)
	{
		Instance.MidrowScorchingManager.OnDrawWithHilight(__instance, g, id, v, flipX, flipY);
	}

	private static void StuffBase_GetTooltips_Postfix(StuffBase __instance, ref List<Tooltip> __result)
	{
		Instance.MidrowScorchingManager.ModifyMidrowObjectTooltips(__instance, __result);
	}

	private static void StuffBase_GetActionsOnDestroyed_Postfix(StuffBase __instance, State __0, Combat __1, bool __2 /* wasPlayer */, ref List<CardAction>? __result)
	{
		__result ??= [];
		Instance.MidrowScorchingManager.ModifyMidrowObjectDestroyedActions(__0, __1, __instance, __2, __result);
	}
}
