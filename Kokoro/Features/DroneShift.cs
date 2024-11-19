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

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	public IDroneShiftHook VanillaDroneShiftHook
		=> DroneShiftManager.Instance.HookMapper.MapToV1(Kokoro.VanillaDroneShiftHook.Instance);

	public IDroneShiftHook VanillaDebugDroneShiftHook
		=> DroneShiftManager.Instance.HookMapper.MapToV1(Kokoro.VanillaDebugDroneShiftHook.Instance);

	public void RegisterDroneShiftHook(IDroneShiftHook hook, double priority)
		=> DroneShiftManager.Instance.HookManager.Register(hook, priority);

	public void UnregisterDroneShiftHook(IDroneShiftHook hook)
		=> DroneShiftManager.Instance.HookManager.Unregister(hook);

	public bool IsDroneShiftPossible(State state, Combat combat, int direction, DroneShiftHookContext context)
		=> DroneShiftManager.Instance.IsDroneShiftPossible(state, combat, direction, context);

	public bool IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> DroneShiftManager.Instance.IsDroneShiftPossible(state, combat, 0, context);

	public IDroneShiftHook? GetDroneShiftHandlingHook(State state, Combat combat, int direction, DroneShiftHookContext context)
		=> DroneShiftManager.Instance.GetHandlingHook(state, combat, direction, context) is { } hook ? DroneShiftManager.Instance.HookMapper.MapToV1(hook) : null;

	public IDroneShiftHook? GetDroneShiftHandlingHook(State state, Combat combat, DroneShiftHookContext context)
		=> DroneShiftManager.Instance.GetHandlingHook(state, combat, 0, context) is { } hook ? DroneShiftManager.Instance.HookMapper.MapToV1(hook) : null;

	public void AfterDroneShift(State state, Combat combat, int direction, IDroneShiftHook hook)
		=> DroneShiftManager.Instance.AfterDroneShift(state, combat, direction, DroneShiftManager.Instance.HookMapper.MapToV2(hook));
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IDroneShiftHookApi DroneShiftHook { get; } = new DroneShiftHookApi();
		
		public sealed class DroneShiftHookApi : IKokoroApi.IV2.IDroneShiftHookApi
		{
			public IKokoroApi.IV2.IDroneShiftHookApi.IHook VanillaDroneShiftHook
				=> Kokoro.VanillaDroneShiftHook.Instance;
			
			public IKokoroApi.IV2.IDroneShiftHookApi.IHook VanillaDebugDroneShiftHook
				=> Kokoro.VanillaDebugDroneShiftHook.Instance;

			public void RegisterHook(IKokoroApi.IV2.IDroneShiftHookApi.IHook hook, double priority = 0)
				=> DroneShiftManager.Instance.HookManager.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IDroneShiftHookApi.IHook hook)
				=> DroneShiftManager.Instance.HookManager.Unregister(hook);
			
			internal record struct IsDroneShiftPossibleArgs(
				State State,
				Combat Combat,
				int Direction,
				IKokoroApi.IV2.IDroneShiftHookApi.DroneShiftHookContext Context
			) : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPossibleArgs;
			
			internal record struct PayForDroneShiftArgs(
				State State,
				Combat Combat,
				int Direction
			) : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IPayForDroneShiftArgs;
			
			internal record struct AfterDroneShiftArgs(
				State State,
				Combat Combat,
				int Direction,
				IKokoroApi.IV2.IDroneShiftHookApi.IHook Hook
			) : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IAfterDroneShiftArgs;
			
			internal record struct ProvideDroneShiftActionsArgs(
				State State,
				Combat Combat,
				int Direction
			) : IKokoroApi.IV2.IDroneShiftHookApi.IHook.IProvideDroneShiftActionsArgs;
		}
	}
}

internal sealed class DroneShiftManager
{
	internal static readonly DroneShiftManager Instance = new();
	internal readonly BidirectionalHookMapper<IKokoroApi.IV2.IDroneShiftHookApi.IHook, IDroneShiftHook> HookMapper = new(
		hook => new V1ToV2DroneShiftHookWrapper(hook),
		hook => new V2ToV1DroneShiftHookWrapper(hook)
	);
	internal readonly VariedApiVersionHookManager<IKokoroApi.IV2.IDroneShiftHookApi.IHook, IDroneShiftHook> HookManager;
	
	public DroneShiftManager()
	{
		HookManager = new(ModEntry.Instance.Package.Manifest.UniqueName, HookMapper);
		
		HookManager.Register(VanillaDroneShiftHook.Instance, 0);
		HookManager.Register(VanillaDebugDroneShiftHook.Instance, 1_000_000_000);
		HookManager.Register(VanillaMidrowCheckDroneShiftHook.Instance, 2_000_000_000);
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

	public IKokoroApi.IV2.IDroneShiftHookApi.IHook? GetHandlingHook(State state, Combat combat, int direction, DroneShiftHookContext context = DroneShiftHookContext.Action)
	{
		foreach (var hook in HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.IsDroneShiftPossible(new ApiImplementation.V2Api.DroneShiftHookApi.IsDroneShiftPossibleArgs(state, combat, direction, (IKokoroApi.IV2.IDroneShiftHookApi.DroneShiftHookContext)(int)context));
			if (hookResult == false)
				return null;
			if (hookResult == true)
				return hook;
		}
		return null;
	}

	public void AfterDroneShift(State state, Combat combat, int direction, IKokoroApi.IV2.IDroneShiftHookApi.IHook hook)
	{
		foreach (var hooks in HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			hooks.AfterDroneShift(new ApiImplementation.V2Api.DroneShiftHookApi.AfterDroneShiftArgs(state, combat, direction, hook));
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
			combat.Queue(hook.ProvideDroneShiftActions(new ApiImplementation.V2Api.DroneShiftHookApi.ProvideDroneShiftActionsArgs(g.state, combat, dir)) ?? [new ADroneMove { dir = dir }]);
			hook.PayForDroneShift(new ApiImplementation.V2Api.DroneShiftHookApi.PayForDroneShiftArgs(g.state, combat, dir));
			Instance.AfterDroneShift(g.state, combat, dir, hook);
		}
		return false;
	}
}

public sealed class VanillaDroneShiftHook : IKokoroApi.IV2.IDroneShiftHookApi.IHook
{
	public static VanillaDroneShiftHook Instance { get; private set; } = new();

	private VanillaDroneShiftHook() { }

	public bool? IsDroneShiftPossible(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPossibleArgs args)
		=> args.State.ship.Get(Status.droneShift) > 0 ? true : null;

	public void PayForDroneShift(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IPayForDroneShiftArgs args)
		=> args.State.ship.Add(Status.droneShift, -1);
}

public sealed class VanillaDebugDroneShiftHook : IKokoroApi.IV2.IDroneShiftHookApi.IHook
{
	public static VanillaDebugDroneShiftHook Instance { get; private set; } = new();

	private VanillaDebugDroneShiftHook() { }

	public bool? IsDroneShiftPossible(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPossibleArgs args)
		=> FeatureFlags.Debug && Input.shift ? true : null;
}

public sealed class VanillaMidrowCheckDroneShiftHook : IKokoroApi.IV2.IDroneShiftHookApi.IHook
{
	public static VanillaMidrowCheckDroneShiftHook Instance { get; private set; } = new();

	private VanillaMidrowCheckDroneShiftHook() { }

	public bool? IsDroneShiftPossible(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPossibleArgs args)
	{
		if (args.Context == IKokoroApi.IV2.IDroneShiftHookApi.DroneShiftHookContext.Action)
			return null;
		if (args.Combat.stuff.Count != 0)
			return null;
		if (args.Combat.stuff.Any(s => !s.Value.Immovable()))
			return null;
		return false;
	}
}

internal sealed class V1ToV2DroneShiftHookWrapper(IDroneShiftHook v1) : IKokoroApi.IV2.IDroneShiftHookApi.IHook
{
	public bool? IsDroneShiftPossible(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPossibleArgs args)
		=> v1.IsDroneShiftPossible(args.State, args.Combat, args.Direction, (DroneShiftHookContext)(int)args.Context);
	
	public void PayForDroneShift(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IPayForDroneShiftArgs args)
		=> v1.PayForDroneShift(args.State, args.Combat, args.Direction);
	
	public void AfterDroneShift(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IAfterDroneShiftArgs args)
		=> v1.AfterDroneShift(args.State, args.Combat, args.Direction, DroneShiftManager.Instance.HookMapper.MapToV1(args.Hook));
	
	public List<CardAction>? ProvideDroneShiftActions(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IProvideDroneShiftActionsArgs args)
		=> v1.ProvideDroneShiftActions(args.State, args.Combat, args.Direction);
}

internal sealed class V2ToV1DroneShiftHookWrapper(IKokoroApi.IV2.IDroneShiftHookApi.IHook v2) : IDroneShiftHook
{
	public bool? IsDroneShiftPossible(State state, Combat combat, int direction, DroneShiftHookContext context)
		=> v2.IsDroneShiftPossible(new ApiImplementation.V2Api.DroneShiftHookApi.IsDroneShiftPossibleArgs(state, combat, direction, (IKokoroApi.IV2.IDroneShiftHookApi.DroneShiftHookContext)(int)context));
	
	public void PayForDroneShift(State state, Combat combat, int direction)
		=> v2.PayForDroneShift(new ApiImplementation.V2Api.DroneShiftHookApi.PayForDroneShiftArgs(state, combat, direction));
	
	public void AfterDroneShift(State state, Combat combat, int direction, IDroneShiftHook hook)
		=> v2.AfterDroneShift(new ApiImplementation.V2Api.DroneShiftHookApi.AfterDroneShiftArgs(state, combat, direction, DroneShiftManager.Instance.HookMapper.MapToV2(hook)));

	public List<CardAction>? ProvideDroneShiftActions(State state, Combat combat, int direction)
		=> v2.ProvideDroneShiftActions(new ApiImplementation.V2Api.DroneShiftHookApi.ProvideDroneShiftActionsArgs(state, combat, direction));
}