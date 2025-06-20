using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Dracula;

internal static class CardSelectFiltersExt
{
	public static ACardSelect SetFilterTemporarilyUpgraded(this ACardSelect self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "TemporarilyUpgraded", value);
		return self;
	}
	
	public static CardBrowse SetFilterTemporarilyUpgraded(this CardBrowse self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "TemporarilyUpgraded", value);
		return self;
	}
}

internal static class CardSelectFilters
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
	}

	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "TemporarilyUpgraded") is { } filterTemporarilyUpgraded)
			ModEntry.Instance.Helper.ModData.SetModData(route, "TemporarilyUpgraded", filterTemporarilyUpgraded);
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, ref List<Card> __result)
	{
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "TemporarilyUpgraded") is { } filterTempUpgraded)
		{
			for (var i = __result.Count - 1; i >= 0; i--)
				if (ModEntry.Instance.KokoroApi.TemporaryUpgrades.GetTemporaryUpgrade(__result[i]) is not null != filterTempUpgraded)
					__result.RemoveAt(i);
		}
	}
}
