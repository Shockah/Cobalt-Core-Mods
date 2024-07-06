using HarmonyLib;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System.Linq;
using System.Reflection;

namespace Shockah.MORE;

internal static class CardOfferingFiltersExt
{
	public static ACardOffering SetMinRarity(this ACardOffering self, Rarity? rarity)
	{
		ModEntry.Instance.Helper.ModData.SetOptionalModData(self, "MinRarity", rarity);
		return self;
	}
}

internal sealed class CardOfferingFilters : IRegisterable
{
	private static ACardOffering? ActionContext;

	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(ACardOffering), nameof(ACardOffering.BeginWithRoute)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardOffering_BeginWithRoute_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardOffering_BeginWithRoute_Finalizer))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => typeof(CardReward).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<GetOffering>") && m.ReturnType == typeof(bool)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Delegate_Postfix))
		);
	}

	private static void ACardOffering_BeginWithRoute_Prefix(ACardOffering __instance)
		=> ActionContext = __instance;

	private static void ACardOffering_BeginWithRoute_Finalizer()
		=> ActionContext = null;

	private static void CardReward_GetOffering_Delegate_Postfix(Card c, ref bool __result)
	{
		if (!__result)
			return;
		if (ActionContext is not { } action)
			return;
		if (ModEntry.Instance.Helper.ModData.GetOptionalModData<Rarity>(action, "MinRarity") is not { } minRarity)
			return;

		if (c.GetMeta().rarity < minRarity)
			__result = false;
	}
}
