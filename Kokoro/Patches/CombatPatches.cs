using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

internal static class CombatPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderMoveButtons)),
			transpiler: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_RenderMoveButtons_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDroneShiftButtons)),
			transpiler: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_RenderDroneShiftButtons_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), "DoEvade"),
			prefix: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_DoEvade_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), "DoDroneShift"),
			prefix: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_DoDroneShift_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_DrainCardActions_Prefix)),
			postfix: new HarmonyMethod(typeof(CombatPatches), nameof(Combat_DrainCardActions_Postfix))
		);
	}

	private static IEnumerable<CodeInstruction> Combat_RenderMoveButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.evade),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt.GetBranchTarget(out var branchTarget)
				)
				.Replace(
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CombatPatches), nameof(Combat_RenderMoveButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget.Value)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool Combat_RenderMoveButtons_Transpiler_ShouldRender(G g)
	{
		if (g.state.route is not Combat combat)
			return false;
		return Instance.EvadeManager.IsEvadePossible(g.state, combat, EvadeHookContext.Rendering);
	}

	private static IEnumerable<CodeInstruction> Combat_RenderDroneShiftButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.droneShift),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt.GetBranchTarget(out var branchTarget)
				)
				.Replace(
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CombatPatches), nameof(Combat_RenderDroneShiftButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget.Value)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool Combat_RenderDroneShiftButtons_Transpiler_ShouldRender(G g)
	{
		if (g.state.route is not Combat combat)
			return false;
		return Instance.DroneShiftManager.IsDroneShiftPossible(g.state, combat, DroneShiftHookContext.Rendering);
	}

	private static bool Combat_DoEvade_Prefix(G g, int dir)
	{
		if (g.state.route is not Combat combat)
			return true;

		var hook = Instance.EvadeManager.GetHandlingHook(g.state, combat);
		if (hook is not null)
		{
			combat.Queue(new AMove
			{
				dir = dir,
				targetPlayer = true,
				fromEvade = true
			});
			hook.PayForEvade(g.state, combat, dir);
			foreach (var hooks in Instance.EvadeManager.GetHooksWithProxies(Instance.Api, g.state.EnumerateAllArtifacts()))
				hooks.AfterEvade(g.state, combat, dir, hook);
		}
		return false;
	}

	private static bool Combat_DoDroneShift_Prefix(G g, int dir)
	{
		if (g.state.route is not Combat combat)
			return true;

		var hook = Instance.DroneShiftManager.GetHandlingHook(g.state, combat);
		if (hook is not null)
		{
			combat.Queue(new ADroneMove
			{
				dir = dir
			});
			hook.PayForDroneShift(g.state, combat, dir);
			foreach (var hooks in Instance.DroneShiftManager.GetHooksWithProxies(Instance.Api, g.state.EnumerateAllArtifacts()))
				hooks.AfterDroneShift(g.state, combat, dir, hook);
		}
		return false;
	}

	private static void Combat_DrainCardActions_Prefix(Combat __instance, ref bool __state)
		=> __state = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;

	private static void Combat_DrainCardActions_Postfix(Combat __instance, ref bool __state)
	{
		var isWorkingOnActions = __instance.currentCardAction is not null || __instance.cardActions.Count != 0;
		if (isWorkingOnActions || !__state)
			return;

		Instance.Api.ObtainExtensionData(__instance, "ContinueFlags", () => new HashSet<Guid>()).Clear();
		Instance.Api.ObtainExtensionData(__instance, "StopFlags", () => new HashSet<Guid>()).Clear();
	}
}
