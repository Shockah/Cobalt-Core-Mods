using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Bjorn;

internal static class AcceleratedCardSelectFiltersExt
{
	public static ACardSelect SetFilterAccelerated(this ACardSelect self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterAccelerated", value);
		return self;
	}
	
	public static CardBrowse SetFilterAccelerated(this CardBrowse self, bool? value)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "FilterAccelerated", value);
		return self;
	}
}

internal sealed class AcceleratedManager : IRegisterable
{
	internal static ISpriteEntry Icon { get; private set; } = null!;

	internal static ICardTraitEntry Trait { get; private set; } = null!;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Accelerated.png"));

		Trait = helper.Content.Cards.RegisterTrait("Accelerated", new()
		{
			Icon = (_, _) => Icon.Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["cardTrait", "Accelerated", "name"]).Localize,
			Tooltips = (_, _) => [
				new GlossaryTooltip($"cardtrait.{package.Manifest.UniqueName}::Accelerated")
				{
					Icon = Icon.Sprite,
					TitleColor = Colors.cardtrait,
					Title = ModEntry.Instance.Localizations.Localize(["cardTrait", "Accelerated", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["cardTrait", "Accelerated", "description"]),
				}
			]
		});

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetCurrentCost)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetCurrentCost_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardSelect), nameof(ACardSelect.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardSelect_BeginWithRoute_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Postfix))
		);
	}

	private static void Card_GetCurrentCost_Postfix(Card __instance, State s, ref int __result)
	{
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(s, __instance, Trait))
			return;
		__result = Math.Max(__result - 1, 0);
	}
	
	private static void ACardSelect_BeginWithRoute_Postfix(ACardSelect __instance, ref Route? __result)
	{
		if (__result is not CardBrowse route)
			return;
		
		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "FilterAccelerated", ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterAccelerated"));
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		var filterAccelerated = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "FilterAccelerated");
		if (filterAccelerated is null)
			return;

		for (var i = __result.Count - 1; i >= 0; i--)
			if (filterAccelerated is not null && ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(g.state, __result[i], Trait) != filterAccelerated.Value)
				__result.RemoveAt(i);
	}
}