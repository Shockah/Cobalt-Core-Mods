using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Natasha;

internal static class CardSelectFiltersExt
{
	public static ACardSelect SetFilterLimited(this ACardSelect self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterLimited", value);
		return self;
	}
}

internal sealed class CardSelectFilters : IRegisterable
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

		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterLimited") is { } filterLimited)
			ModEntry.Instance.Helper.ModData.SetModData(route, "FilterLimited", filterLimited);
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		var filterLimited = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterLimited");
		if (filterLimited is null)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
		{
			if (filterLimited is not null && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(g.state, __result[i], Limited.Trait) != filterLimited.Value)
			{
				__result.RemoveAt(i);
				continue;
			}
		}
	}
}
