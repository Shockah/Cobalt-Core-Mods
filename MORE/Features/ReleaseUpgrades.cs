using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
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
		DB.cardMetas[typeof(ReleaseCard).Name].upgradesTo = [Upgrade.A];
	}

	private static void ReleaseCard_GetData_Postfix(Card __instance, ref CardData __result)
	{
		if (__instance.upgrade != Upgrade.A)
			return;
		__result.singleUse = false;
		__result.exhaust = true;
	}
}
