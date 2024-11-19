using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public IDroneShiftHook VanillaDroneShiftHook
		=> Kokoro.VanillaDroneShiftHook.Instance;

	public IDroneShiftHook VanillaDebugDroneShiftHook
		=> Kokoro.VanillaDebugDroneShiftHook.Instance;

	public void RegisterDroneShiftHook(IDroneShiftHook hook, double priority)
		=> DroneShiftManager.Instance.Register(hook, priority);

	public void UnregisterDroneShiftHook(IDroneShiftHook hook)
		=> DroneShiftManager.Instance.Unregister(hook);

	public bool IsDroneShiftPossible(State state, Combat combat, int direction, DroneShiftHookContext context)
		=> DroneShiftManager.Instance.IsDroneShiftPossible(state, combat, direction, context);

	public bool IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> DroneShiftManager.Instance.IsDroneShiftPossible(state, combat, 0, context);

	public IDroneShiftHook? GetDroneShiftHandlingHook(State state, Combat combat, int direction, DroneShiftHookContext context)
		=> DroneShiftManager.Instance.GetHandlingHook(state, combat, direction, context);

	public IDroneShiftHook? GetDroneShiftHandlingHook(State state, Combat combat, DroneShiftHookContext context)
		=> DroneShiftManager.Instance.GetHandlingHook(state, combat, 0, context);

	public void AfterDroneShift(State state, Combat combat, int direction, IDroneShiftHook hook)
		=> DroneShiftManager.Instance.AfterDroneShift(state, combat, direction, hook);
}

internal sealed class DroneShiftManager : HookManager<IDroneShiftHook>
{
	internal static readonly DroneShiftManager Instance = new();
	
	public DroneShiftManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
		Register(VanillaDroneShiftHook.Instance, 0);
		Register(VanillaDebugDroneShiftHook.Instance, 1_000_000_000);
		Register(VanillaMidrowCheckDroneShiftHook.Instance, 2_000_000_000);
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderDroneShiftButtons)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDroneShiftButtons_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), "DoDroneShift"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DoDroneShift_Prefix))
		);
	}

	public bool IsDroneShiftPossible(State state, Combat combat, int direction, DroneShiftHookContext context)
		=> GetHandlingHook(state, combat, direction, context) is not null;

	public IDroneShiftHook? GetHandlingHook(State state, Combat combat, int direction, DroneShiftHookContext context = DroneShiftHookContext.Action)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.IsDroneShiftPossible(state, combat, direction, context);
			if (hookResult == false)
				return null;
			if (hookResult == true)
				return hook;
		}
		return null;
	}

	public void AfterDroneShift(State state, Combat combat, int direction, IDroneShiftHook hook)
	{
		foreach (var hooks in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			hooks.AfterDroneShift(state, combat, direction, hook);
	}
	
	private static IEnumerable<CodeInstruction> Combat_RenderDroneShiftButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			var leftEndLabel = il.DefineLabel();
			var rightEndLabel = il.DefineLabel();

			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(1).ExtractLabels(out var labels),
					ILMatches.Ldfld("state"),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.droneShift),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt,
					ILMatches.Instruction(OpCodes.Ret)
				])
				.Replace(new CodeInstruction(OpCodes.Nop).WithLabels(labels))
				.Find([
					ILMatches.Ldloc<Combat>(originalMethod),
					ILMatches.Ldfld("stuff"),
					ILMatches.Call("get_Count"),
					ILMatches.Brfalse
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Call("Any"),
					ILMatches.Brtrue,
					ILMatches.Instruction(OpCodes.Ret)
				])
				.Remove()
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldarg(1).Anchor(out var gPointer1),
					ILMatches.Stloc<G>(originalMethod),
					ILMatches.LdcI4((int)StableUK.btn_moveDrones_left),
					ILMatches.Call("op_Implicit")
				])
				.Anchors()
				.PointerMatcher(gPointer1)
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldc_I4, -1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDroneShiftButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brfalse, leftEndLabel),
					new CodeInstruction(OpCodes.Ldarg_1)
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Ldarg(1),
					ILMatches.Stloc<G>(originalMethod),
					ILMatches.LdcI4((int)StableUK.btn_moveDrones_right),
					ILMatches.Call("op_Implicit")
				])
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Encompass(SequenceMatcherEncompassDirection.Before, 3)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(leftEndLabel),
					new CodeInstruction(OpCodes.Ldc_I4, 1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderDroneShiftButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brfalse, rightEndLabel)
				])
				.PointerMatcher(SequenceMatcherRelativeElement.LastInWholeSequence)
				.AddLabel(rightEndLabel)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool Combat_RenderDroneShiftButtons_Transpiler_ShouldRender(G g, int direction)
	{
		if (g.state.route is not Combat combat)
			return false;
		return Instance.IsDroneShiftPossible(g.state, combat, direction, DroneShiftHookContext.Rendering);
	}

	private static bool Combat_DoDroneShift_Prefix(G g, int dir)
	{
		if (g.state.route is not Combat combat)
			return true;

		var hook = Instance.GetHandlingHook(g.state, combat, dir);
		if (hook is not null)
		{
			combat.Queue(hook.ProvideDroneShiftActions(g.state, combat, dir) ?? [new ADroneMove { dir = dir }]);
			hook.PayForDroneShift(g.state, combat, dir);
			Instance.AfterDroneShift(g.state, combat, dir, hook);
		}
		return false;
	}
}

public sealed class VanillaDroneShiftHook : IDroneShiftHook
{
	public static VanillaDroneShiftHook Instance { get; private set; } = new();

	private VanillaDroneShiftHook() { }

	public bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> state.ship.Get(Status.droneShift) > 0 ? true : null;

	public void PayForDroneShift(State state, Combat combat, int direction)
		=> state.ship.Add(Status.droneShift, -1);
}

public sealed class VanillaDebugDroneShiftHook : IDroneShiftHook
{
	public static VanillaDebugDroneShiftHook Instance { get; private set; } = new();

	private VanillaDebugDroneShiftHook() { }

	public bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> FeatureFlags.Debug && Input.shift ? true : null;
}

public sealed class VanillaMidrowCheckDroneShiftHook : IDroneShiftHook
{
	public static VanillaMidrowCheckDroneShiftHook Instance { get; private set; } = new();

	private VanillaMidrowCheckDroneShiftHook() { }

	public bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
	{
		if (context == DroneShiftHookContext.Action)
			return null;
		if (combat.stuff.Count != 0)
			return null;
		if (combat.stuff.Any(s => !s.Value.Immovable()))
			return null;
		return false;
	}
}