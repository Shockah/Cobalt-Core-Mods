using HarmonyLib;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.CustomDifficulties;

internal static class HardmodePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(HARDMODE), nameof(HARDMODE.GetSprite)),
			postfix: new HarmonyMethod(typeof(HardmodePatches), nameof(Hardmode_GetSprite_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(HARDMODE), nameof(HARDMODE.GetExtraTooltips)),
			postfix: new HarmonyMethod(typeof(HardmodePatches), nameof(Hardmode_GetExtraTooltips_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(HARDMODE), nameof(HARDMODE.OnTurnStart)),
			postfix: new HarmonyMethod(typeof(HardmodePatches), nameof(Hardmode_OnTurnStart_Postfix))
		);
	}

	private static void Hardmode_GetSprite_Postfix(HARDMODE __instance, ref Spr __result)
	{
		if (__instance.difficulty == ModEntry.EasyDifficultyLevel)
			__result = Instance.EasyModeArtifactSprite;
	}

	private static void Hardmode_GetExtraTooltips_Postfix(HARDMODE __instance, ref List<Tooltip>? __result)
	{
		if (__instance.difficulty != ModEntry.EasyDifficultyLevel)
			return;

		__result ??= new();
		__result.RemoveAll(t => t is TTText textTooltip && textTooltip.text == "???");
		__result.Add(new TTText(I18n.EasyModeDifficultyTooltip));
	}

	private static void Hardmode_OnTurnStart_Postfix(HARDMODE __instance, Combat combat)
	{
		if (combat.turn < 1)
			return;
		if (__instance.difficulty != ModEntry.EasyDifficultyLevel)
			return;

		combat.QueueImmediate(new AStatus
		{
			status = Status.tempShield,
			statusAmount = 1,
			targetPlayer = true
		});
	}
}
