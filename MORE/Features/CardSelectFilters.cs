using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.MORE;

internal static class CardSelectFiltersExt
{
	public static ACardSelect SetFilterInfinite(this ACardSelect self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterInfinite", value);
		return self;
	}

	public static ACardSelect SetFilterRecycle(this ACardSelect self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterRecycle", value);
		return self;
	}
}

internal sealed class CardSelectFilters : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
	}

	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterInfinite") is { } filterInfinite)
			ModEntry.Instance.Helper.ModData.SetModData(route, "FilterInfinite", filterInfinite);
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterRecycle") is { } filterRecycle)
			ModEntry.Instance.Helper.ModData.SetModData(route, "FilterRecycle", filterRecycle);
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		var filterInfinite = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterInfinite");
		var filterRecycle = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterRecycle");
		if (filterInfinite is null && filterRecycle is null)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
		{
			var data = __result[i].GetDataWithOverrides(g.state);
			if (filterInfinite is not null && data.infinite != filterInfinite.Value)
			{
				__result.RemoveAt(i);
				continue;
			}
			if (filterRecycle is not null && data.recycle != filterRecycle.Value)
			{
				__result.RemoveAt(i);
				continue;
			}
		}
	}
}
