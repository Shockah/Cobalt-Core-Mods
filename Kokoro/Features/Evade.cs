using FSPRO;
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
	public IEvadeHook VanillaEvadeHook
		=> Kokoro.VanillaEvadeHook.Instance;

	public IEvadeHook VanillaDebugEvadeHook
		=> Kokoro.VanillaDebugEvadeHook.Instance;

	public void RegisterEvadeHook(IEvadeHook hook, double priority)
		=> EvadeManager.Instance.Register(hook, priority);

	public void UnregisterEvadeHook(IEvadeHook hook)
		=> EvadeManager.Instance.Unregister(hook);

	public bool IsEvadePossible(State state, Combat combat, int direction, EvadeHookContext context)
		=> EvadeManager.Instance.IsEvadePossible(state, combat, direction, context);

	public bool IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> EvadeManager.Instance.IsEvadePossible(state, combat, 0, context);

	public IEvadeHook? GetEvadeHandlingHook(State state, Combat combat, int direction, EvadeHookContext context)
		=> EvadeManager.Instance.GetHandlingHook(state, combat, direction, context);

	public IEvadeHook? GetEvadeHandlingHook(State state, Combat combat, EvadeHookContext context)
		=> EvadeManager.Instance.GetHandlingHook(state, combat, 0, context);

	public void AfterEvade(State state, Combat combat, int direction, IEvadeHook hook)
		=> EvadeManager.Instance.AfterEvade(state, combat, direction, hook);
}

internal sealed class EvadeManager : HookManager<IEvadeHook>
{
	internal static readonly EvadeManager Instance = new();
	
	public EvadeManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
		Register(VanillaEvadeHook.Instance, 0);
		Register(VanillaDebugEvadeHook.Instance, 1_000_000_000);
		Register(VanillaTrashAnchorEvadeHook.Instance, 1000);
		Register(VanillaLockdownEvadeHook.Instance, 1001);
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

	public IEvadeHook? GetHandlingHook(State state, Combat combat, int direction, EvadeHookContext context = EvadeHookContext.Action)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.IsEvadePossible(state, combat, direction, context);
			if (hookResult == false)
				return null;
			if (hookResult == true)
				return hook;
		}
		return null;
	}

	public void AfterEvade(State state, Combat combat, int direction, IEvadeHook hook)
	{
		foreach (var hooks in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			hooks.AfterEvade(state, combat, direction, hook);
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
			combat.Queue(hook.ProvideEvadeActions(g.state, combat, dir) ?? [new AMove
			{
				dir = dir,
				targetPlayer = true,
				fromEvade = true
			}]);
			hook.PayForEvade(g.state, combat, dir);
			Instance.AfterEvade(g.state, combat, dir, hook);
		}
		return false;
	}
}

public sealed class VanillaEvadeHook : IEvadeHook
{
	public static VanillaEvadeHook Instance { get; private set; } = new();

	private VanillaEvadeHook() { }

	public bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> state.ship.Get(Status.evade) > 0 ? true : null;

	public void PayForEvade(State state, Combat combat, int direction)
		=> state.ship.Add(Status.evade, -1);
}

public sealed class VanillaDebugEvadeHook : IEvadeHook
{
	public static VanillaDebugEvadeHook Instance { get; private set; } = new();

	private VanillaDebugEvadeHook() { }

	public bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> FeatureFlags.Debug && Input.shift ? true : null;
}

public sealed class VanillaTrashAnchorEvadeHook : IEvadeHook
{
	public static VanillaTrashAnchorEvadeHook Instance { get; private set; } = new();

	private VanillaTrashAnchorEvadeHook() { }

	public bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context)
	{
		if (context != EvadeHookContext.Action)
			return null;
		if (!combat.hand.Any(c => c is TrashAnchor))
			return null;

		Audio.Play(Event.Status_PowerDown);
		state.ship.shake += 1.0;
		return false;
	}

	public void PayForEvade(State state, Combat combat, int direction)
		=> state.ship.Add(Status.evade, -1);
}

public sealed class VanillaLockdownEvadeHook : IEvadeHook
{
	public static VanillaLockdownEvadeHook Instance { get; private set; } = new();

	private VanillaLockdownEvadeHook() { }

	public bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context)
	{
		if (context != EvadeHookContext.Action)
			return null;
		if (state.ship.Get(Status.lockdown) <= 0)
			return null;

		Audio.Play(Event.Status_PowerDown);
		state.ship.shake += 1.0;
		return false;
	}

	public void PayForEvade(State state, Combat combat, int direction)
		=> state.ship.Add(Status.evade, -1);
}