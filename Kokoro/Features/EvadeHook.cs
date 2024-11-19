using FSPRO;
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
	
	public IEvadeHook VanillaEvadeHook
		=> EvadeManager.Instance.HookMapper.MapToV1(Kokoro.VanillaEvadeHook.Instance);

	public IEvadeHook VanillaDebugEvadeHook
		=> EvadeManager.Instance.HookMapper.MapToV1(Kokoro.VanillaDebugEvadeHook.Instance);

	public void RegisterEvadeHook(IEvadeHook hook, double priority)
		=> EvadeManager.Instance.HookManager.Register(hook, priority);

	public void UnregisterEvadeHook(IEvadeHook hook)
		=> EvadeManager.Instance.HookManager.Unregister(hook);

	public bool IsEvadePossible(State state, Combat combat, int direction, EvadeHookContext context)
		=> EvadeManager.Instance.IsEvadePossible(state, combat, direction, context);

	public bool IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> EvadeManager.Instance.IsEvadePossible(state, combat, 0, context);

	public IEvadeHook? GetEvadeHandlingHook(State state, Combat combat, int direction, EvadeHookContext context)
		=> EvadeManager.Instance.GetHandlingHook(state, combat, direction, context) is { } hook ? EvadeManager.Instance.HookMapper.MapToV1(hook) : null;

	public IEvadeHook? GetEvadeHandlingHook(State state, Combat combat, EvadeHookContext context)
		=> EvadeManager.Instance.GetHandlingHook(state, combat, 0, context) is { } hook ? EvadeManager.Instance.HookMapper.MapToV1(hook) : null;

	public void AfterEvade(State state, Combat combat, int direction, IEvadeHook hook)
		=> EvadeManager.Instance.AfterEvade(state, combat, direction, EvadeManager.Instance.HookMapper.MapToV2(hook));
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IEvadeHookApi EvadeHook { get; } = new EvadeHookApi();
		
		public sealed class EvadeHookApi : IKokoroApi.IV2.IEvadeHookApi
		{
			public IKokoroApi.IV2.IEvadeHookApi.IHook VanillaEvadeHook
				=> Kokoro.VanillaEvadeHook.Instance;
			
			public IKokoroApi.IV2.IEvadeHookApi.IHook VanillaDebugEvadeHook
				=> Kokoro.VanillaDebugEvadeHook.Instance;

			public void RegisterHook(IKokoroApi.IV2.IEvadeHookApi.IHook hook, double priority = 0)
				=> EvadeManager.Instance.HookManager.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IEvadeHookApi.IHook hook)
				=> EvadeManager.Instance.HookManager.Unregister(hook);
			
			internal record struct IsEvadePossibleArgs(
				State State,
				Combat Combat,
				int Direction,
				IKokoroApi.IV2.IEvadeHookApi.EvadeHookContext Context
			) : IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePossibleArgs;
			
			internal record struct PayForEvadeArgs(
				State State,
				Combat Combat,
				int Direction
			) : IKokoroApi.IV2.IEvadeHookApi.IHook.IPayForEvadeArgs;
			
			internal record struct AfterEvadeArgs(
				State State,
				Combat Combat,
				int Direction,
				IKokoroApi.IV2.IEvadeHookApi.IHook Hook
			) : IKokoroApi.IV2.IEvadeHookApi.IHook.IAfterEvadeArgs;
			
			internal record struct ProvideEvadeActionsArgs(
				State State,
				Combat Combat,
				int Direction
			) : IKokoroApi.IV2.IEvadeHookApi.IHook.IProvideEvadeActionsArgs;
		}
	}
}

internal sealed class EvadeManager
{
	internal static readonly EvadeManager Instance = new();
	internal readonly BidirectionalHookMapper<IKokoroApi.IV2.IEvadeHookApi.IHook, IEvadeHook> HookMapper = new(
		hook => new V1ToV2EvadeHookWrapper(hook),
		hook => new V2ToV1EvadeHookWrapper(hook)
	);
	internal readonly VariedApiVersionHookManager<IKokoroApi.IV2.IEvadeHookApi.IHook, IEvadeHook> HookManager;
	
	public EvadeManager()
	{
		HookManager = new(ModEntry.Instance.Package.Manifest.UniqueName, HookMapper);
		
		HookManager.Register(VanillaEvadeHook.Instance, 0);
		HookManager.Register(VanillaDebugEvadeHook.Instance, 1_000_000_000);
		HookManager.Register(VanillaTrashAnchorEvadeHook.Instance, 1000);
		HookManager.Register(VanillaLockdownEvadeHook.Instance, 1001);
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderMoveButtons)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderMoveButtons_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), "DoEvade"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DoEvade_Prefix))
		);
	}

	public bool IsEvadePossible(State state, Combat combat, int direction, EvadeHookContext context)
		=> GetHandlingHook(state, combat, direction, context) is not null;

	public IKokoroApi.IV2.IEvadeHookApi.IHook? GetHandlingHook(State state, Combat combat, int direction, EvadeHookContext context = EvadeHookContext.Action)
	{
		foreach (var hook in HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.IsEvadePossible(new ApiImplementation.V2Api.EvadeHookApi.IsEvadePossibleArgs(state, combat, direction, (IKokoroApi.IV2.IEvadeHookApi.EvadeHookContext)(int)context));
			if (hookResult == false)
				return null;
			if (hookResult == true)
				return hook;
		}
		return null;
	}

	public void AfterEvade(State state, Combat combat, int direction, IKokoroApi.IV2.IEvadeHookApi.IHook hook)
	{
		foreach (var hooks in HookManager.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			hooks.AfterEvade(new ApiImplementation.V2Api.EvadeHookApi.AfterEvadeArgs(state, combat, direction, hook));
	}
	
	private static IEnumerable<CodeInstruction> Combat_RenderMoveButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
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
					ILMatches.LdcI4((int)Status.evade),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Bgt,
					ILMatches.Instruction(OpCodes.Ret)
				])
				.Replace(new CodeInstruction(OpCodes.Nop).WithLabels(labels))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldarg(1).Anchor(out var gPointer1),
					ILMatches.LdcI4((int)StableUK.btn_move_left),
					ILMatches.AnyCall,
					ILMatches.Stloc<UIKey>(originalMethod)
				])
				.Anchors()
				.PointerMatcher(gPointer1)
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldc_I4, -1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderMoveButtons_Transpiler_ShouldRender))),
					new CodeInstruction(OpCodes.Brfalse, leftEndLabel),
					new CodeInstruction(OpCodes.Ldarg_1)
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Ldarg(1),
					ILMatches.LdcI4((int)StableUK.btn_move_right),
					ILMatches.AnyCall,
					ILMatches.Stloc<UIKey>(originalMethod)
				])
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Encompass(SequenceMatcherEncompassDirection.Before, 3)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1).WithLabels(leftEndLabel),
					new CodeInstruction(OpCodes.Ldc_I4, 1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderMoveButtons_Transpiler_ShouldRender))),
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

	private static bool Combat_RenderMoveButtons_Transpiler_ShouldRender(G g, int direction)
	{
		if (g.state.route is not Combat combat)
			return false;
		return Instance.IsEvadePossible(g.state, combat, direction, EvadeHookContext.Rendering);
	}
	
	private static bool Combat_DoEvade_Prefix(G g, int dir)
	{
		if (g.state.route is not Combat combat)
			return true;

		var hook = Instance.GetHandlingHook(g.state, combat, dir);
		if (hook is not null)
		{
			combat.Queue(hook.ProvideEvadeActions(new ApiImplementation.V2Api.EvadeHookApi.ProvideEvadeActionsArgs(g.state, combat, dir)) ?? [new AMove
			{
				dir = dir,
				targetPlayer = true,
				fromEvade = true
			}]);
			hook.PayForEvade(new ApiImplementation.V2Api.EvadeHookApi.PayForEvadeArgs(g.state, combat, dir));
			Instance.AfterEvade(g.state, combat, dir, hook);
		}
		return false;
	}
}

public sealed class VanillaEvadeHook : IKokoroApi.IV2.IEvadeHookApi.IHook
{
	public static VanillaEvadeHook Instance { get; private set; } = new();

	private VanillaEvadeHook() { }

	public bool? IsEvadePossible(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePossibleArgs args)
		=> args.State.ship.Get(Status.evade) > 0 ? true : null;

	public void PayForEvade(IKokoroApi.IV2.IEvadeHookApi.IHook.IPayForEvadeArgs args)
		=> args.State.ship.Add(Status.evade, -1);
}

public sealed class VanillaDebugEvadeHook : IKokoroApi.IV2.IEvadeHookApi.IHook
{
	public static VanillaDebugEvadeHook Instance { get; private set; } = new();

	private VanillaDebugEvadeHook() { }

	public bool? IsEvadePossible(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePossibleArgs args)
		=> FeatureFlags.Debug && Input.shift ? true : null;
}

public sealed class VanillaTrashAnchorEvadeHook : IKokoroApi.IV2.IEvadeHookApi.IHook
{
	public static VanillaTrashAnchorEvadeHook Instance { get; private set; } = new();

	private VanillaTrashAnchorEvadeHook() { }

	public bool? IsEvadePossible(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePossibleArgs args)
	{
		if (args.Context != IKokoroApi.IV2.IEvadeHookApi.EvadeHookContext.Action)
			return null;
		if (!args.Combat.hand.Any(c => c is TrashAnchor))
			return null;

		Audio.Play(Event.Status_PowerDown);
		args.State.ship.shake += 1.0;
		return false;
	}

	public void PayForEvade(IKokoroApi.IV2.IEvadeHookApi.IHook.IPayForEvadeArgs args)
		=> args.State.ship.Add(Status.evade, -1);
}

public sealed class VanillaLockdownEvadeHook : IKokoroApi.IV2.IEvadeHookApi.IHook
{
	public static VanillaLockdownEvadeHook Instance { get; private set; } = new();

	private VanillaLockdownEvadeHook() { }

	public bool? IsEvadePossible(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePossibleArgs args)
	{
		if (args.Context != IKokoroApi.IV2.IEvadeHookApi.EvadeHookContext.Action)
			return null;
		if (args.State.ship.Get(Status.lockdown) <= 0)
			return null;

		Audio.Play(Event.Status_PowerDown);
		args.State.ship.shake += 1.0;
		return false;
	}

	public void PayForEvade(IKokoroApi.IV2.IEvadeHookApi.IHook.IPayForEvadeArgs args)
		=> args.State.ship.Add(Status.evade, -1);
}

internal sealed class V1ToV2EvadeHookWrapper(IEvadeHook v1) : IKokoroApi.IV2.IEvadeHookApi.IHook
{
	public bool? IsEvadePossible(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePossibleArgs args)
		=> v1.IsEvadePossible(args.State, args.Combat, args.Direction, (EvadeHookContext)(int)args.Context);
	
	public void PayForEvade(IKokoroApi.IV2.IEvadeHookApi.IHook.IPayForEvadeArgs args)
		=> v1.PayForEvade(args.State, args.Combat, args.Direction);
	
	public void AfterEvade(IKokoroApi.IV2.IEvadeHookApi.IHook.IAfterEvadeArgs args)
		=> v1.AfterEvade(args.State, args.Combat, args.Direction, EvadeManager.Instance.HookMapper.MapToV1(args.Hook));
	
	public List<CardAction>? ProvideEvadeActions(IKokoroApi.IV2.IEvadeHookApi.IHook.IProvideEvadeActionsArgs args)
		=> v1.ProvideEvadeActions(args.State, args.Combat, args.Direction);
}

internal sealed class V2ToV1EvadeHookWrapper(IKokoroApi.IV2.IEvadeHookApi.IHook v2) : IEvadeHook
{
	public bool? IsEvadePossible(State state, Combat combat, int direction, EvadeHookContext context)
		=> v2.IsEvadePossible(new ApiImplementation.V2Api.EvadeHookApi.IsEvadePossibleArgs(state, combat, direction, (IKokoroApi.IV2.IEvadeHookApi.EvadeHookContext)(int)context));
	
	public void PayForEvade(State state, Combat combat, int direction)
		=> v2.PayForEvade(new ApiImplementation.V2Api.EvadeHookApi.PayForEvadeArgs(state, combat, direction));
	
	public void AfterEvade(State state, Combat combat, int direction, IEvadeHook hook)
		=> v2.AfterEvade(new ApiImplementation.V2Api.EvadeHookApi.AfterEvadeArgs(state, combat, direction, EvadeManager.Instance.HookMapper.MapToV2(hook)));

	public List<CardAction>? ProvideEvadeActions(State state, Combat combat, int direction)
		=> v2.ProvideEvadeActions(new ApiImplementation.V2Api.EvadeHookApi.ProvideEvadeActionsArgs(state, combat, direction));
}