using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

internal static class AVariableHintPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AVariableHint), nameof(AVariableHint.GetTooltips)),
			postfix: new HarmonyMethod(typeof(AVariableHintPatches), nameof(AVariableHint_GetTooltips_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AVariableHint), nameof(AVariableHint.GetIcon)),
			postfix: new HarmonyMethod(typeof(AVariableHintPatches), nameof(AVariableHint_GetIcon_Postfix))
		);
	}

	private static void AVariableHint_GetTooltips_Postfix(AVariableHint __instance, State s, ref List<Tooltip> __result)
	{
		if (__instance.hand)
			return;
		if (__instance.status is not { } status)
			return;

		int index = __result.FindIndex(t => t is TTGlossary glossary && glossary.key == "action.xHint.desc");
		if (index < 0)
			return;

		if (Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
		{
			__result[index] = new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => null,
				() => "",
				() => I18n.EnergyVariableHint,
				new Func<object>[]
				{
					() => (s.route is Combat combat) ? $" </c>(<c=keyword>{Instance.Api.ObtainExtensionData(__instance, "energyTooltipOverride", () => (int?)null) ?? combat.energy}</c>)" : ""
				}
			);
		}
		else if (!Instance.Api.ObtainExtensionData(__instance, "targetPlayer", () => true))
		{
			__result[index] = new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => null,
				() => "",
				() => I18n.EnemyVariableHint,
				new Func<object>[]
				{
					() => "<c=status>" + status.GetLocName().ToUpperInvariant() + "</c>",
					() => (s.route is Combat combat1) ? $" </c>(<c=keyword>{combat1.otherShip.Get(status)}</c>)" : "",
					() => __instance.secondStatus is { } secondStatus1 ? (" </c>+ <c=status>" + secondStatus1.GetLocName().ToUpperInvariant() + "</c>") : "",
					() => __instance.secondStatus is { } secondStatus2 && s.route is Combat combat2 ? $" </c>(<c=keyword>{combat2.otherShip.Get(secondStatus2)}</c>)" : ""
				}
			);
		}
	}

	private static void AVariableHint_GetIcon_Postfix(AVariableHint __instance, ref Icon? __result)
	{
		if (!Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
			return;
		__result = new(
			path: (Spr)Instance.Content.EnergySprite.Id!.Value,
			number: null,
			color: Colors.white
		);
	}
}
