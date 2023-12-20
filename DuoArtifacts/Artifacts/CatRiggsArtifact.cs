using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class CatRiggsArtifact : DuoArtifact
{
	public bool DoingInitialDraw = false;
	public bool WaitingForFirstExtraDraw = true;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToHand)),
			transpiler: new HarmonyMethod(GetType(), nameof(Combat_SendCardToHand_Transpiler))
		);
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		DoingInitialDraw = true;
		WaitingForFirstExtraDraw = true;
	}

	public override void OnDrawCard(State state, Combat combat, int count)
	{
		base.OnDrawCard(state, combat, count);
		if (DoingInitialDraw)
		{
			DoingInitialDraw = false;
			return;
		}
	}

	private static IEnumerable<CodeInstruction> Combat_SendCardToHand_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("hand"),
					ILMatches.Call("get_Count"),
					ILMatches.LdcI4(10)
				)
				.Find(ILMatches.Blt.GetBranchTarget(out var branchTarget))
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CatRiggsArtifact), nameof(Combat_SendCardToHand_Transpiler_DidDrawCard)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Combat_SendCardToHand_Transpiler_DidDrawCard(State state, Card card)
	{
		var artifact = state.EnumerateAllArtifacts().OfType<CatRiggsArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		if (artifact.DoingInitialDraw)
			return;
		if (!artifact.WaitingForFirstExtraDraw)
			return;
		if (card.GetCurrentCost(state) <= 0)
			return;

		card.discount--;
		artifact.Pulse();
		artifact.WaitingForFirstExtraDraw = false;
	}
}