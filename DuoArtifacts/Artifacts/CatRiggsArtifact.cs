using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.DuoArtifacts;

internal sealed class CatRiggsArtifact : DuoArtifact
{
	public bool DoingInitialDraw;
	public bool WaitingForFirstExtraDraw = true;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.SendCardToHand)),
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
		DoingInitialDraw = false;
	}

	private static IEnumerable<CodeInstruction> Combat_SendCardToHand_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
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
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void Combat_SendCardToHand_Transpiler_DidDrawCard(State state, Card card)
	{
		if (state.EnumerateAllArtifacts().OfType<CatRiggsArtifact>().FirstOrDefault() is not { } artifact)
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