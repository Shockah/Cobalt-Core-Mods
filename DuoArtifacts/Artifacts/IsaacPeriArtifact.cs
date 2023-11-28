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

internal sealed class IsaacPeriArtifact : DuoArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private static State? TooltipState;
	private static State? ActionState;
	private static int? LastLibra;
	private static int? LastOverdrive;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), nameof(AAttack.Begin)),
			transpiler: new HarmonyMethod(GetType(), nameof(AAttack_Begin_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AAttack), "DoLibraEffect"),
			transpiler: new HarmonyMethod(GetType(), nameof(AAttack_DoLibraEffect_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_GetAllTooltips_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Card_GetAllTooltips_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDrones)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_RenderDrones_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Combat_RenderDrones_Finalizer))
		);

		// this doesn't work, the method gets inlined; transpile `GetActions` and `GetTooltips` instead
		//harmony.TryPatch(
		//	logger: Instance.Logger!,
		//	original: () => AccessTools.DeclaredMethod(typeof(AttackDrone), "AttackDamage"),
		//	postfix: new HarmonyMethod(GetType(), nameof(AttackDrone_AttackDamage_Postfix))
		//);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AttackDrone), nameof(AttackDrone.GetActions)),
			prefix: new HarmonyMethod(GetType(), nameof(AttackDrone_GetActions_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AttackDrone_GetActions_Finalizer)),
			transpiler: new HarmonyMethod(GetType(), nameof(AttackDrone_GetActions_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AttackDrone), nameof(AttackDrone.GetTooltips)),
			transpiler: new HarmonyMethod(GetType(), nameof(AttackDrone_GetTooltips_Transpiler))
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

	private static int GetModifiedAttackDamage(int damage, AttackDrone drone)
	{
		if ((ActionState ?? TooltipState) is not { } state)
			return damage;
		if (drone.targetPlayer)
			return damage;

		var artifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is IsaacPeriArtifact);
		if (artifact is null)
			return damage;

		if (state == ActionState)
			artifact.Pulse();
		damage += state.ship.Get(Status.powerdrive);
		damage += LastOverdrive ?? state.ship.Get(Status.overdrive);
		return damage;
	}

	private static IEnumerable<CodeInstruction> AAttack_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldloc<Ship>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.LdcI4((int)Status.libra),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Ble
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.ExtractBranchTarget(out var branchTarget)
				.PointerMatcher(branchTarget)
				.Find(ILMatches.Stloc<bool>(originalMethod.GetMethodBody()!.LocalVariables))
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLdlocInstruction(out var ldlocLibraFlag)
				.CreateStlocInstruction(out var stlocLibraFlag)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					ldlocLibraFlag,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacPeriArtifact), nameof(AAttack_Begin_Transpiler_Modify))),
					stlocLibraFlag
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static bool AAttack_Begin_Transpiler_Modify(AAttack attack, State state, bool libraFlag)
	{
		if (libraFlag)
			return true;

		var artifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is IsaacPeriArtifact);
		if (artifact is null)
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
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(2),
					ILMatches.LdcI4((int)Status.libra),
					ILMatches.Call("Get")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacPeriArtifact), nameof(AAttack_DoLibraEffect_Transpiler_Modify)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static int AAttack_DoLibraEffect_Transpiler_Modify(int amount, Ship ship)
	{
		if (LastLibra is null)
			return amount;
		return amount + LastLibra.Value - ship.Get(Status.libra);
	}

	private static void AttackDrone_GetActions_Prefix(State s)
		=> ActionState = s;

	private static void AttackDrone_GetActions_Finalizer()
		=> ActionState = null;

	private static void Card_GetAllTooltips_Prefix(State s)
		=> TooltipState = s;

	private static void Card_GetAllTooltips_Finalizer()
		=> TooltipState = null;

	private static void Combat_RenderDrones_Prefix(G g)
		=> TooltipState = g.state;

	private static void Combat_RenderDrones_Finalizer()
		=> TooltipState = null;

	private static IEnumerable<CodeInstruction> AttackDrone_GetActions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("AttackDamage"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacPeriArtifact), nameof(GetModifiedAttackDamage)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static IEnumerable<CodeInstruction> AttackDrone_GetTooltips_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.ForEach(
					SequenceMatcherRelativeBounds.WholeSequence,
					new IElementMatch<CodeInstruction>[]
					{
						ILMatches.Call("AttackDamage")
					},
					matcher =>
					{
						return matcher
							.Insert(
								SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
								new CodeInstruction(OpCodes.Ldarg_0),
								new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(IsaacPeriArtifact), nameof(GetModifiedAttackDamage)))
							);
					},
					minExpectedOccurences: 2,
					maxExpectedOccurences: 2
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}
}
