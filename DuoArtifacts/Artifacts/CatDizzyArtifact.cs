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

internal sealed class CatDizzyArtifact : DuoArtifact
{
	public bool TriggeredThisCombat = false;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			transpiler: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Prefix))
		);
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		TriggeredThisCombat = false;
	}

	private static void Trigger(State state)
	{
		var artifact = state.EnumerateAllArtifacts().OfType<CatDizzyArtifact>().FirstOrDefault();
		if (artifact is null || artifact.TriggeredThisCombat)
			return;

		artifact.Pulse();
		state.ship.Add(Status.perfectShield, state.ship.Get(Status.shield) + 2);
		state.ship.Set(Status.shield, 0);
		state.ship.Set(Status.maxShield, -state.ship.shieldMaxBase);
		artifact.TriggeredThisCombat = true;
	}

	private static IEnumerable<CodeInstruction> Ship_NormalDamage_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(3),
					ILMatches.Stloc<int>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.CreateLdlocInstruction(out var ldlocRemainingDamage)

				.Find(
					ILMatches.Ldarg(5),
					ILMatches.Brtrue
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocRemainingDamage,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CatDizzyArtifact), nameof(Ship_NormalDamage_Transpiler_ApplyPerfectShieldIfNeeded)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Ship_NormalDamage_Transpiler_ApplyPerfectShieldIfNeeded(Ship ship, State state, int remainingDamage)
	{
		if (ship != state.ship)
			return;
		if (remainingDamage <= ship.Get(Status.tempShield))
			return;
		Trigger(state);
	}

	private static void Ship_DirectHullDamage_Prefix(Ship __instance, State s, int amt)
	{
		if (__instance != s.ship)
			return;
		if (amt <= 0)
			return;
		Trigger(s);
	}
}