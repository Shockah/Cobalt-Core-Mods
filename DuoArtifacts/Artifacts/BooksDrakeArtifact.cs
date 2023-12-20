using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.DuoArtifacts;

internal sealed class BooksDrakeArtifact : DuoArtifact
{
	private const int ShardCost = 2;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			transpiler: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0).ExtractLabels(out var labels),
					ILMatches.Ldfld("stunEnemy"),
					ILMatches.Brtrue
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BooksDrakeArtifact), nameof(AAttack_Begin_Transpiler_Modify)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void AAttack_Begin_Transpiler_Modify(AAttack attack, State state, Combat combat)
	{
		if (attack.targetPlayer || attack.fromDroneX is not null)
			return;
		var artifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDrakeArtifact);
		if (artifact is null)
			return;

		if (state.ship.Get(Status.shard) < ShardCost)
			return;

		artifact.Pulse();
		combat.QueueImmediate(new AStatus
		{
			status = Status.shard,
			statusAmount = -2,
			targetPlayer = true
		});

		if (!attack.piercing)
		{
			attack.piercing = true;
			return;
		}
		if (!attack.stunEnemy)
		{
			attack.stunEnemy = true;
			return;
		}

		combat.Queue(new AStunShip());
	}
}
