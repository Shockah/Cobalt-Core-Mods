using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.MORE;

internal sealed class ReleaseUpgrades : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(ReleaseCard), nameof(ReleaseCard.GetData)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ReleaseCard_GetData_Postfix)))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(ReleaseCard), nameof(ReleaseCard.GetActions)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ReleaseCard_GetActions_Postfix)))
		);
	}

	public static void UpdateSettings(IPluginPackage<IModManifest> package, IModHelper helper, ProfileSettings settings)
	{
		DB.cardMetas[typeof(ReleaseCard).Name].upgradesTo = settings.EnabledReleaseUpgrades ? [Upgrade.A] : [];
	}

	private static void ReleaseCard_GetData_Postfix(Card __instance, ref CardData __result)
	{
		if (ModEntry.Instance.Settings.ProfileBased.Current.EnabledFlippableRelease)
			__result.flippable = true;

		if (__instance.upgrade != Upgrade.A)
			return;

		__result.singleUse = false;
		__result.exhaust = true;
	}

	private static void ReleaseCard_GetActions_Postfix(Card __instance, ref List<CardAction> __result)
	{
		if (!__instance.flipped)
			return;

		foreach (var action in __result)
			if (action is ASpawn spawnAction)
				spawnAction.thing.targetPlayer = !spawnAction.thing.targetPlayer;
	}
}
