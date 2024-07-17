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
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
			transpiler: new HarmonyMethod(typeof(AStatusPatches), nameof(AStatus_Begin_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetTooltips)),
			postfix: new HarmonyMethod(typeof(AStatusPatches), nameof(AStatus_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetIcon)),
			postfix: new HarmonyMethod(typeof(AStatusPatches), nameof(AStatus_GetIcon_Postfix))
		);
	}

	private static IEnumerable<CodeInstruction> AStatus_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<Ship>(originalMethod),
					ILMatches.LdcI4((int)Status.boost),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Ble,
					ILMatches.Ldarg(0).Anchor(out var replaceStartAnchor),
					ILMatches.Ldfld("status"),
					ILMatches.LdcI4((int)Status.shield),
					ILMatches.Beq,
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("status"),
					ILMatches.LdcI4((int)Status.tempShield),
					ILMatches.Beq.GetBranchTarget(out var branchTarget)
				)
				.Anchors().EncompassUntil(replaceStartAnchor)
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AStatusPatches), nameof(AStatus_Begin_Transpiler_ShouldApplyBoost))),
					new CodeInstruction(OpCodes.Brfalse, branchTarget.Value)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool AStatus_Begin_Transpiler_ShouldApplyBoost(AStatus status, State state, Combat combat)
	{
		var ship = status.targetPlayer ? state.ship : combat.otherShip;
		return Instance.StatusLogicManager.IsAffectedByBoost(state, combat, ship, status.status);
	}

	private static void AStatus_GetTooltips_Postfix(AStatus __instance, State s, ref List<Tooltip> __result)
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
