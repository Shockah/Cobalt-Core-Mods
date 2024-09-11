using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

internal static class AStatusPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetTooltips)),
			postfix: new HarmonyMethod(typeof(AStatusPatches), nameof(AStatus_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetIcon)),
			postfix: new HarmonyMethod(typeof(AStatusPatches), nameof(AStatus_GetIcon_Postfix))
		);
	}

	private static void AStatus_GetTooltips_Postfix(AStatus __instance, ref List<Tooltip> __result)
	{
		if (!Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
			return;

		__result.Clear();
		__result.Add(new CustomTTGlossary(
			CustomTTGlossary.GlossaryType.status,
			() => (Spr)Instance.Content.EnergySprite.Id!.Value,
			() => I18n.EnergyGlossaryName,
			() => I18n.EnergyGlossaryDescription,
			key: "AStatus.Energy"
		));
	}

	private static void AStatus_GetIcon_Postfix(AStatus __instance, ref Icon? __result)
	{
		if (!Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
			return;
		__result = new(
			path: (Spr)Instance.Content.EnergySprite.Id!.Value,
			number: __instance.mode == AStatusMode.Set ? null : __instance.statusAmount,
			color: Colors.white
		);
	}
}
