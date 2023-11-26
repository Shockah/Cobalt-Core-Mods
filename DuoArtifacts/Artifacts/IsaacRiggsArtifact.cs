using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class IsaacRiggsArtifact : DuoArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderMoveButtons)),
			transpiler: new HarmonyMethod(typeof(IsaacRiggsArtifact), nameof(Combat_RenderMoveButtons_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDroneShiftButtons)),
			transpiler: new HarmonyMethod(typeof(IsaacRiggsArtifact), nameof(Combat_RenderDroneShiftButtons_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), "DoEvade"),
			transpiler: new HarmonyMethod(typeof(IsaacRiggsArtifact), nameof(Combat_DoEvade_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), "DoDroneShift"),
			transpiler: new HarmonyMethod(typeof(IsaacRiggsArtifact), nameof(Combat_DoDroneShift_Transpiler))
		);
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 1)
			combat.QueueImmediate(new AStatus
			{
				status = Status.evade,
				statusAmount = 1,
				targetPlayer = true
			});
	}

	private static IEnumerable<CodeInstruction> Combat_RenderMoveButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.evade),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.ExtractBranchTarget(out var branchTarget)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacRiggsArtifact), nameof(Combat_RenderDroneShiftButtons_Transpiler_ShouldRenderMoveButtonsAnyway))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget),
					new CodeInstruction(OpCodes.Ret)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool Combat_RenderDroneShiftButtons_Transpiler_ShouldRenderMoveButtonsAnyway(G g)
		=> g.state.artifacts.Any(a => a is IsaacRiggsArtifact) && g.state.ship.Get(Status.droneShift) > 0;

	private static IEnumerable<CodeInstruction> Combat_RenderDroneShiftButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.droneShift),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.ExtractBranchTarget(out var branchTarget)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacRiggsArtifact), nameof(Combat_RenderDroneShiftButtons_Transpiler_ShouldRenderDroneShiftButtonsAnyway))),
					new CodeInstruction(OpCodes.Brtrue, branchTarget),
					new CodeInstruction(OpCodes.Ret)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool Combat_RenderDroneShiftButtons_Transpiler_ShouldRenderDroneShiftButtonsAnyway(G g)
		=> g.state.artifacts.Any(a => a is IsaacRiggsArtifact) && g.state.ship.Get(Status.evade) > 0;

	private static IEnumerable<CodeInstruction> Combat_DoEvade_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AsGuidAnchorable()

				.Find(
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.evade),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Instruction(OpCodes.Cgt)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacRiggsArtifact), nameof(Combat_DoEvade_Transpiler_ShouldAllowMoveAnyway))),
					new CodeInstruction(OpCodes.Or)
				)

				.Find(
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.evade).WithAutoAnchor(out Guid statusAnchor),
					ILMatches.LdcI4(-1),
					ILMatches.Call("Add")
				)
				.PointerMatcher(statusAnchor)
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacRiggsArtifact), nameof(Combat_DoEvade_Transpiler_GetCorrectStatusToReduce)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool Combat_DoEvade_Transpiler_ShouldAllowMoveAnyway(G g)
		=> g.state.artifacts.Any(a => a is IsaacRiggsArtifact) && g.state.ship.Get(Status.droneShift) > 0;

	private static int Combat_DoEvade_Transpiler_GetCorrectStatusToReduce(G g)
		=> (int)(g.state.ship.Get(Status.evade) > 0 ? Status.evade : Status.droneShift);

	private static IEnumerable<CodeInstruction> Combat_DoDroneShift_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AsGuidAnchorable()

				.Find(
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.droneShift),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Instruction(OpCodes.Cgt)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacRiggsArtifact), nameof(Combat_DoDroneShift_Transpiler_ShouldAllowDroneShiftAnyway))),
					new CodeInstruction(OpCodes.Or)
				)

				.Find(
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.droneShift).WithAutoAnchor(out Guid statusAnchor),
					ILMatches.LdcI4(-1),
					ILMatches.Call("Add")
				)
				.PointerMatcher(statusAnchor)
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacRiggsArtifact), nameof(Combat_DoDroneShift_Transpiler_GetCorrectStatusToReduce)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool Combat_DoDroneShift_Transpiler_ShouldAllowDroneShiftAnyway(G g)
		=> g.state.artifacts.Any(a => a is IsaacRiggsArtifact) && g.state.ship.Get(Status.evade) > 0;

	private static int Combat_DoDroneShift_Transpiler_GetCorrectStatusToReduce(G g)
		=> (int)(g.state.ship.Get(Status.droneShift) > 0 ? Status.droneShift : Status.evade);
}