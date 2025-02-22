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

internal sealed class DizzyDrakeArtifact : DuoArtifact
{
	private const int ExtraShieldDamage = 1;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AOverheat), nameof(AOverheat.Begin)),
			transpiler: new HarmonyMethod(GetType(), nameof(AOverheat_Begin_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> AOverheat_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("DirectHullDamage"))
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(DizzyDrakeArtifact), nameof(AOverheat_Begin_Transpiler_Damage))))
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void AOverheat_Begin_Transpiler_Damage(Ship ship, State state, Combat combat, int damage)
	{
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is DizzyDrakeArtifact) is not { } artifact || ship != state.ship)
		{
			ship.DirectHullDamage(state, combat, damage);
			return;
		}

		var totalShield = ship.Get(Status.shield) + ship.Get(Status.tempShield);
		if (state.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			totalShield += ship.Get(Status.shard);

		if (totalShield < damage + ExtraShieldDamage)
		{
			ship.DirectHullDamage(state, combat, damage);
			return;
		}

		artifact.Pulse();
		ship.NormalDamage(state, combat, damage + ExtraShieldDamage, maybeWorldGridX: null);
	}
}
