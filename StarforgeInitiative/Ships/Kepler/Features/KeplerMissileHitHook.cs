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
			var continueLabel = il.DefineLabel();
			
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
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
					new CodeInstruction(OpCodes.Ldarg_2).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_3),
					ldlocMissile,
					new CodeInstruction(OpCodes.Ldarg_0),
					ldlocRay,
					ldlocaDamage,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMissileHit_Update_Transpiler_ModifyHit))),
					new CodeInstruction(OpCodes.Brtrue, continueLabel),
					new CodeInstruction(OpCodes.Ret),
				])
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.AddLabel(continueLabel)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool AMissileHit_Update_Transpiler_ModifyHit(State state, Combat combat, Missile missile, AMissileHit action, RaycastResult ray, ref int damage)
	{
		var ship = action.targetPlayer ? state.ship : combat.otherShip;
		var @continue = true;
		foreach (var artifact in state.EnumerateAllArtifacts())
		{
			if (artifact is not IKeplerMissileHitHook hook)
				continue;
			if (hook.OnMissileHit(state, combat, ship, missile, action, ray, ref @continue, ref damage))
				break;
		}
		return @continue;
	}
}

public interface IKeplerMissileHitHook
{
	bool OnMissileHit(State state, Combat combat, Ship ship, Missile missile, AMissileHit action, RaycastResult ray, ref bool @continue, ref int damage);
}