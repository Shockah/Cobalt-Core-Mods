using HarmonyLib;
using Nickel;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public AVariableHint MakeEnergyX(AVariableHint? action = null, bool energy = true, int? tooltipOverride = null)
		{
			var copy = action is null ? new() : Mutil.DeepCopy(action);
			copy.status = Status.tempShield; // it doesn't matter, but it has to be *anything*
			Instance.Api.SetExtensionData(copy, "energy", energy);
			Instance.Api.SetExtensionData(copy, "energyTooltipOverride", tooltipOverride);
			return copy;
		}

		public AStatus MakeEnergy(AStatus action, bool energy = true)
		{
			var copy = Mutil.DeepCopy(action);
			copy.targetPlayer = true;
			Instance.Api.SetExtensionData(copy, "energy", energy);
			return copy;
		}
	}
}

internal sealed class EnergyAsStatusManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetIcon)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_GetIcon_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AVariableHint), nameof(AVariableHint.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AVariableHint_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AVariableHint), nameof(AVariableHint.GetIcon)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AVariableHint_GetIcon_Postfix))
		);
	}
	
	private static void AStatus_GetTooltips_Postfix(AStatus __instance, ref List<Tooltip> __result)
	{
		if (!ModEntry.Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
			return;

		__result.Clear();
		__result.Add(new GlossaryTooltip("AStatus.Energy")
		{
			Icon = (Spr)ModEntry.Instance.Content.EnergySprite.Id!.Value,
			TitleColor = Colors.energy,
			Title = I18n.EnergyGlossaryName,
			Description = I18n.EnergyGlossaryDescription
		});
	}

	private static void AStatus_GetIcon_Postfix(AStatus __instance, ref Icon? __result)
	{
		if (!ModEntry.Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
			return;
		__result = new(
			path: (Spr)ModEntry.Instance.Content.EnergySprite.Id!.Value,
			number: __instance.mode == AStatusMode.Set ? null : __instance.statusAmount,
			color: Colors.white
		);
	}
	
	private static void AVariableHint_GetTooltips_Postfix(AVariableHint __instance, State s, ref List<Tooltip> __result)
	{
		if (__instance.hand)
			return;
		if (__instance.status is null)
			return;

		var index = __result.FindIndex(t => t is TTGlossary { key: "action.xHint.desc" });
		if (index < 0)
			return;
		if (!ModEntry.Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
			return;

		__result[index] = new GlossaryTooltip("AStatus.Energy")
		{
			Description = I18n.EnergyVariableHint,
			vals = [
				(s.route is Combat combat) ? $" </c>(<c=keyword>{ModEntry.Instance.Api.ObtainExtensionData(__instance, "energyTooltipOverride", () => (int?)null) ?? combat.energy}</c>)" : ""
			]
		};
	}

	private static void AVariableHint_GetIcon_Postfix(AVariableHint __instance, ref Icon? __result)
	{
		if (!ModEntry.Instance.Api.ObtainExtensionData(__instance, "energy", () => false))
			return;
		__result = new(
			path: (Spr)ModEntry.Instance.Content.EnergySprite.Id!.Value,
			number: null,
			color: Colors.white
		);
	}
}