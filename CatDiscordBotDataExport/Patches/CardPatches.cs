using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.CatDiscordBotDataExport;

internal sealed class CardPatches
{
	private static ModEntry Instance => ModEntry.Instance;
	internal static bool ActivateAllActions = false;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActionsOverridden)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActionsOverridden_Postfix))
		);
	}

	private static void Card_GetActionsOverridden_Postfix(Card __instance, ref List<CardAction> __result)
	{
		if (ActivateAllActions)
			foreach (var action in __result)
				action.disabled = false;
	}
}