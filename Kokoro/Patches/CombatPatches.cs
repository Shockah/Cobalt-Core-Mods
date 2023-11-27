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
	}

	private static IEnumerable<CodeInstruction> Combat_RenderMoveButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AsGuidAnchorable()
				.Find(
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.evade),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt
				)
				.AnchorBlock(out Guid findAnchor)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.ExtractBranchTarget(out var branchTarget)
				.BlockMatcher(findAnchor)
				.Replace(
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CombatPatches), nameof(Combat_RenderMoveButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget)
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
		return Instance.EvadeHookManager.IsEvadePossible(g.state, combat, EvadeHookContext.Rendering);
	}

	private static IEnumerable<CodeInstruction> Combat_RenderDroneShiftButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AsGuidAnchorable()
				.Find(
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.droneShift),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt
				)
				.AnchorBlock(out Guid findAnchor)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.ExtractBranchTarget(out var branchTarget)
				.BlockMatcher(findAnchor)
				.Replace(
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CombatPatches), nameof(Combat_RenderDroneShiftButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget)
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
		return Instance.DroneShiftHookManager.IsDroneShiftPossible(g.state, combat, DroneShiftHookContext.Rendering);
	}

	private static bool Combat_DoEvade_Prefix(G g, int dir)
	{
		if (g.state.route is not Combat combat)
			return true;

		var hook = Instance.EvadeHookManager.GetHandlingHook(g.state, combat);
		if (hook is not null)
		{
			combat.Queue(new AMove
			{
				dir = dir,
				targetPlayer = true,
				fromEvade = true
			});
			hook.PayForEvade(g.state, combat, dir);
			foreach (var hooks in Instance.EvadeHookManager)
				hooks.AfterEvade(g.state, combat, dir);
		}
		return false;
	}

	private static bool Combat_DoDroneShift_Prefix(G g, int dir)
	{
		if (g.state.route is not Combat combat)
			return true;

		var hook = Instance.DroneShiftHookManager.GetHandlingHook(g.state, combat);
		if (hook is not null)
		{
			combat.Queue(new ADroneMove
			{
				dir = dir
			});
			hook.PayForDroneShift(g.state, combat, dir);
			foreach (var hooks in Instance.EvadeHookManager)
				hooks.AfterEvade(g.state, combat, dir);
		}
		return false;
	}
}
