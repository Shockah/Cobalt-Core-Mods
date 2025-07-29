using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.DuoArtifacts;

internal sealed class IsaacPeriArtifact : DuoArtifact
{
	private static int? LastLibra;
	private static int? LastOverdrive;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			transpiler: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AAttack), "DoLibraEffect"),
			transpiler: new HarmonyMethod(GetType(), nameof(AAttack_DoLibraEffect_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AttackDrone), "AttackDamage"),
			postfix: new HarmonyMethod(GetType(), nameof(AttackDrone_AttackDamage_Postfix))
		);
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		LastLibra = null;
		LastOverdrive = null;
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		LastLibra = state.ship.Get(Status.libra);
		LastOverdrive = state.ship.Get(Status.overdrive);
	}

	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<Ship>(originalMethod),
					ILMatches.LdcI4((int)Status.libra),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Ble.GetBranchTarget(out var branchTarget)
				])
				.PointerMatcher(branchTarget)
				.Find(ILMatches.Stloc<bool>(originalMethod).CreateLdlocInstruction(out var ldlocLibraFlag).CreateStlocInstruction(out var stlocLibraFlag))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					ldlocLibraFlag,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacPeriArtifact), nameof(AAttack_Begin_Transpiler_Modify))),
					stlocLibraFlag
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool AAttack_Begin_Transpiler_Modify(AAttack attack, State state, bool libraFlag)
	{
		if (libraFlag)
			return true;
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is IsaacPeriArtifact) is not { } artifact)
			return false;
		if (attack.targetPlayer)
			return false;
		if ((LastLibra ?? state.ship.Get(Status.libra)) <= 0)
			return false;

		artifact.Pulse();
		return true;
	}

	private static IEnumerable<CodeInstruction> AAttack_DoLibraEffect_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(2),
					ILMatches.LdcI4((int)Status.libra),
					ILMatches.Call("Get")
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacPeriArtifact), nameof(AAttack_DoLibraEffect_Transpiler_Modify)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static int AAttack_DoLibraEffect_Transpiler_Modify(int amount, Ship ship)
	{
		if (LastLibra is null)
			return amount;
		return amount + LastLibra.Value - ship.Get(Status.libra);
	}

	private static void AttackDrone_AttackDamage_Postfix(AttackDrone __instance, ref int __result)
	{
		if (__instance.targetPlayer)
			return;
		if (MG.inst?.g?.state is not { } state)
			return;
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is IsaacPeriArtifact) is null)
			return;
		
		__result += state.ship.Get(Status.powerdrive);
		__result += LastOverdrive ?? state.ship.Get(Status.overdrive);
	}
}
