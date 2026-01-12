using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.StarforgeInitiative;

internal sealed class KeplerMissileHitHookManager : IRegisterable
{
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMissileHit), nameof(AMissileHit.Update)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> AMissileHit_Update_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(3).CreateLabel(il, out var destroyLabel),
					ILMatches.Ldfld("stuff"),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("worldX"),
					ILMatches.Call("Remove"),
				])
				.BlockMatcher(SequenceMatcherRelativeBounds.WholeSequence)
				.Find([
					ILMatches.Isinst<Missile>(),
					ILMatches.Stloc<Missile>(originalMethod).CreateLdlocInstruction(out var ldlocMissile),
				])
				.Find([
					ILMatches.Call("RaycastGlobal"),
					ILMatches.Stloc<RaycastResult>(originalMethod).CreateLdlocInstruction(out var ldlocRay),
				])
				.Find([
					ILMatches.Ldloc<int>(originalMethod).CreateLdlocaInstruction(out var ldlocaDamage).ExtractLabels(out var labels),
					ILMatches.LdcI4(0),
					ILMatches.Bge,
					ILMatches.LdcI4(0),
					ILMatches.Stloc<int>(originalMethod),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					ldlocMissile,
					new CodeInstruction(OpCodes.Ldarg_0),
					ldlocRay,
					ldlocaDamage,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler_ModifyHit))),
					new CodeInstruction(OpCodes.Brfalse, destroyLabel),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool AMissileHit_Update_Transpiler_ModifyHit(G g, State state, Combat combat, Missile missile, AMissileHit action, RaycastResult ray, ref int damage)
	{
		var ship = missile.targetPlayer ? state.ship : combat.otherShip;
		var @continue = true;
		
		foreach (var artifact in state.EnumerateAllArtifacts())
		{
			if (artifact is not IKeplerMissileHitHook hook)
				continue;
			if (hook.OnMissileHit(state, combat, ship, missile, action, ray, ref @continue, ref damage))
				break;
		}

		if (!@continue)
		{
			state.AddShake(1.0);
			Input.Rumble(0.5);
			EffectSpawner.NonCannonHit(g, missile.targetPlayer, ray, new() { hitShield = true });
			
			foreach (var artifact in state.EnumerateAllArtifacts())
				artifact.OnPlayerDestroyDrone(state, combat);
		}
		
		return @continue;
	}
}

public interface IKeplerMissileHitHook
{
	bool OnMissileHit(State state, Combat combat, Ship ship, Missile missile, AMissileHit action, RaycastResult ray, ref bool @continue, ref int damage);
}