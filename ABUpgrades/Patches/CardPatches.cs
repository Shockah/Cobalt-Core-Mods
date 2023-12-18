using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.ABUpgrades;

internal static class CardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetFullDisplayName)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_GetFullDisplayName_Postfix_Last)), priority: Priority.Last)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.IsUpgradable)),
			postfix: new HarmonyMethod(typeof(CardPatches), nameof(Card_IsUpgradable_Postfix))
		);
	}

	public static void ApplyLate(Harmony harmony)
	{
		harmony.TryPatchVirtual(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetData)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_GetData_Prefix_Last)), priority: Priority.Last)
		);
		harmony.TryPatchVirtual(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActions)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Card_GetActions_Prefix_Last)), priority: Priority.Last)
		);
	}

	private static void Card_GetFullDisplayName_Postfix_Last(Card __instance, ref string __result)
	{
		if (__instance.upgrade != ABUpgradeManager.ABUpgrade)
			return;
		if (!__result.EndsWith(" ?"))
			return;
		__result = $"{__result[..^2]} AB";
	}

	private static void Card_IsUpgradable_Postfix(Card __instance, ref bool __result)
	{
		if (__result)
			return;
		if (__instance.upgrade is not Upgrade.A or Upgrade.B)
			return;
		__result = Instance.Manager.HasABUpgrade(__instance);
	}

	private static bool Card_GetData_Prefix_Last(Card __instance, State __0, ref CardData __result)
	{
		if (__instance.upgrade != ABUpgradeManager.ABUpgrade)
			return true;

		var @override = Instance.Manager.GetABUpgradeData(__0, __instance);
		if (@override is null)
			return true;

		__result = @override.Value;
		return false;
	}

	private static bool Card_GetActions_Prefix_Last(Card __instance, State __0, Combat __1, ref List<CardAction> __result)
	{
		if (__instance.upgrade != ABUpgradeManager.ABUpgrade)
			return true;

		var @override = Instance.Manager.GetABUpgradeActions(__0, __1, __instance);
		if (@override is null)
			return true;

		__result = @override;
		return false;
	}
}
