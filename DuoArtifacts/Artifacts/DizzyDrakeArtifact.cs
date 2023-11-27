﻿using HarmonyLib;
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

internal sealed class DizzyDrakeArtifact : DuoArtifact
{
	private static ModEntry Instance => ModEntry.Instance;

	private const int ExtraShieldDamage = 1;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AOverheat), nameof(AOverheat.Begin)),
			transpiler: new HarmonyMethod(typeof(DizzyDrakeArtifact), nameof(AOverheat_Begin_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> AOverheat_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
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
	}

	private static void AOverheat_Begin_Transpiler_Damage(Ship ship, State state, Combat combat, int damage)
	{
		var artifact = state.artifacts.FirstOrDefault(a => a is DizzyDrakeArtifact);
		bool doNormalDamage = ship == state.ship && artifact is not null && ship.Get(Status.shield) + ship.Get(Status.tempShield) >= damage + ExtraShieldDamage;

		if (doNormalDamage)
		{
			artifact?.Pulse();
			ship.NormalDamage(state, combat, damage + ExtraShieldDamage, -999, worldSpaceAgnostic: true);
		}
		else
		{
			ship.DirectHullDamage(state, combat, damage);
		}
	}
}