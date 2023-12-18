using HarmonyLib;
using Shockah.Shared;

namespace Shockah.ABUpgrades;

internal static class DBExtenderPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(AccessTools.TypeByName("CobaltCoreModding.Components.Services.DBExtender, CobaltCoreModding.Components"), "PatchMetasAndStoryFunctions"),
			postfix: new HarmonyMethod(typeof(DBExtenderPatches), nameof(DBExtender_InitSync_Postfix))
		);
	}

	private static void DBExtender_InitSync_Postfix()
	{
		foreach (var (key, type) in DB.cards)
			Instance.Manager.ApplyMetaChange(key, type);
	}
}