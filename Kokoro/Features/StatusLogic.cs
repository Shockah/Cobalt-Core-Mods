﻿using HarmonyLib;
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
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1

	private static readonly ConditionalWeakTable<IStatusLogicHook, IKokoroApi.IV2.IStatusLogicApi.IHook> V1ToV2StatusLogicHookWrappers = [];

	private sealed class V1ToV2StatusLogicHookWrapper(IStatusLogicHook v1) : IKokoroApi.IV2.IStatusLogicApi.IHook
	{
		public int ModifyStatusChange(State state, Combat combat, Ship ship, Status status, int oldAmount, int newAmount)
			=> v1.ModifyStatusChange(state, combat, ship, status, oldAmount, newAmount); 
		
		public bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status)
			=> v1.IsAffectedByBoost(state, combat, ship, status);

		public void OnStatusTurnTrigger(State state, Combat combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount)
			=> v1.OnStatusTurnTrigger(state, combat, (StatusTurnTriggerTiming)(int)timing, ship, status, oldAmount, newAmount);

		public bool HandleStatusTurnAutoStep(State state, Combat combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy setStrategy)
		{
			var v1SetStrategy = (StatusTurnAutoStepSetStrategy)(int)setStrategy;
			var result = v1.HandleStatusTurnAutoStep(state, combat, (StatusTurnTriggerTiming)(int)timing, ship, status, ref amount, ref v1SetStrategy);
			setStrategy = (IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy)(int)v1SetStrategy;
			return result;
		}
	}
	
	public void RegisterStatusLogicHook(IStatusLogicHook hook, double priority)
		=> StatusLogicManager.Instance.Register(V1ToV2StatusLogicHookWrappers.GetValue(hook, key => new V1ToV2StatusLogicHookWrapper(key)), priority);

	public void UnregisterStatusLogicHook(IStatusLogicHook hook)
	{
		if (V1ToV2StatusLogicHookWrappers.TryGetValue(hook, out var wrapper))
			StatusLogicManager.Instance.Unregister(wrapper);
	}

	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IStatusLogicApi StatusLogic { get; } = new StatusLogicApi();
		
		public sealed class StatusLogicApi : IKokoroApi.IV2.IStatusLogicApi
		{
			public void RegisterHook(IKokoroApi.IV2.IStatusLogicApi.IHook hook, double priority = 0)
				=> StatusLogicManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IStatusLogicApi.IHook hook)
				=> StatusLogicManager.Instance.Unregister(hook);
		}
	}
}

internal sealed class StatusLogicManager : HookManager<IKokoroApi.IV2.IStatusLogicApi.IHook>
{
	internal static readonly StatusLogicManager Instance = new();
	
	private static readonly Dictionary<Status, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy> AutoStepSetStrategies = new()
	{
		{ Status.stunCharge, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct },
		{ Status.tempShield, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct },
		{ Status.tempPayback, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct },
		{ Status.autododgeLeft, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct },
		{ Status.autododgeRight, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct },
		{ Status.autopilot, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct },
		{ Status.hermes, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct },
		{ Status.engineStall, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct },
		{ Status.lockdown, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueAdd },
	};

	public StatusLogicManager()
	{
		Register(VanillaBoostStatusLogicHook.Instance, 0);
		Register(VanillaTimestopStatusLogicHook.Instance, 1_000_000);
		Register(VanillaTurnStartStatusAutoStepLogicHook.Instance, 10);
		Register(VanillaTurnEndStatusAutoStepLogicHook.Instance, 10);
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnBeginTurn_Prefix_First)), Priority.First),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnBeginTurn_Transpiler_Last)), Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnAfterTurn)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnAfterTurn_Prefix_First)), Priority.First),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnAfterTurn_Transpiler_Last)), Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_Set_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.Begin)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_Begin_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RevengeDrive), nameof(RevengeDrive.OnPlayerLoseHull)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RevengeDrive_OnPlayerLoseHull_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RevengeDrive_OnPlayerLoseHull_Postfix))
		);
	}

	public int ModifyStatusChange(State state, Combat combat, Ship ship, Status status, int oldAmount, int newAmount)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
			newAmount = hook.ModifyStatusChange(state, combat, ship, status, oldAmount, newAmount);
		return newAmount;
	}

	public bool IsAffectedByBoost(State state, Combat combat, Ship ship, Status status)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
			if (hook.IsAffectedByBoost(state, combat, ship, status) is { } result)
				return result;
		return true;
	}

	internal void OnTurnStart(State state, Combat combat, Ship ship)
		=> OnTurnStartOrEnd(state, combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart, ship);

	internal void OnTurnEnd(State state, Combat combat, Ship ship)
		=> OnTurnStartOrEnd(state, combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd, ship);

	private void OnTurnStartOrEnd(State state, Combat combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming timing, Ship ship)
	{
		var oldAmounts = DB.statuses.Keys
			.ToDictionary(status => status, ship.Get);

		foreach (var (status, oldAmount) in oldAmounts)
		{
			var newAmount = oldAmount;
			var setStrategy = AutoStepSetStrategies.GetValueOrDefault(status, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueImmediateAdd);

			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
				if (hook.HandleStatusTurnAutoStep(state, combat, timing, ship, status, ref newAmount, ref setStrategy))
					break;
			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
				hook.OnStatusTurnTrigger(state, combat, timing, ship, status, oldAmount, newAmount);

			if (newAmount == oldAmount)
				continue;

			switch (setStrategy)
			{
				case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct:
					ship.Set(status, newAmount);
					break;
				case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueSet:
					combat.Queue(new AStatus
					{
						targetPlayer = ship.isPlayerShip,
						status = status,
						mode = AStatusMode.Set,
						statusAmount = newAmount
					});
					break;
				case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueAdd:
					combat.Queue(new AStatus
					{
						targetPlayer = ship.isPlayerShip,
						status = status,
						mode = AStatusMode.Add,
						statusAmount = newAmount - oldAmount
					});
					break;
				case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueImmediateSet:
					combat.QueueImmediate(new AStatus
					{
						targetPlayer = ship.isPlayerShip,
						status = status,
						mode = AStatusMode.Set,
						statusAmount = newAmount
					});
					break;
				case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueImmediateAdd:
					combat.QueueImmediate(new AStatus
					{
						targetPlayer = ship.isPlayerShip,
						status = status,
						mode = AStatusMode.Add,
						statusAmount = newAmount - oldAmount
					});
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
	
	private static void Ship_OnBeginTurn_Prefix_First(Ship __instance, State s, Combat c)
		=> Instance.OnTurnStart(s, c, __instance);

	private static IEnumerable<CodeInstruction> Ship_OnBeginTurn_Transpiler_Last(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.LdcI4((int)Status.timeStop),
					ILMatches.Call("Get")
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Pop),
					new CodeInstruction(OpCodes.Ldc_I4_1)
				])
				.Find(ILMatches.Call("QueueImmediate"))
				.Replace([
					new CodeInstruction(OpCodes.Pop),
					new CodeInstruction(OpCodes.Pop)
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}
	
	private static void Ship_OnAfterTurn_Prefix_First(Ship __instance, State s, Combat c)
		=> Instance.OnTurnEnd(s, c, __instance);

	private static IEnumerable<CodeInstruction> Ship_OnAfterTurn_Transpiler_Last(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.LdcI4((int)Status.timeStop),
					ILMatches.Call("Get")
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Pop),
					new CodeInstruction(OpCodes.Ldc_I4_1)
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}
	
	private static bool Ship_Set_Prefix(Ship __instance, Status status, ref int n)
	{
		if (MG.inst.g.state is not { } state)
			return true;
		if (state.route is not Combat combat)
			return true;

		var oldAmount = __instance.Get(status);
		var newAmount = Instance.ModifyStatusChange(state, combat, __instance, status, oldAmount, n);

		if (newAmount == oldAmount)
			return false;
		n = newAmount;
		return true;
	}
	
	private static IEnumerable<CodeInstruction> AStatus_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<Ship>(originalMethod),
					ILMatches.LdcI4((int)Status.boost),
					ILMatches.Call("Get"),
					ILMatches.LdcI4(0),
					ILMatches.Ble,
					ILMatches.Ldarg(0).Anchor(out var replaceStartAnchor),
					ILMatches.Ldfld("status"),
					ILMatches.LdcI4((int)Status.shield),
					ILMatches.Beq,
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("status"),
					ILMatches.LdcI4((int)Status.tempShield),
					ILMatches.Beq.GetBranchTarget(out var branchTarget)
				])
				.Anchors().EncompassUntil(replaceStartAnchor)
				.Replace([
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(AStatus_Begin_Transpiler_ShouldApplyBoost))),
					new CodeInstruction(OpCodes.Brfalse, branchTarget.Value)
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool AStatus_Begin_Transpiler_ShouldApplyBoost(AStatus status, State state, Combat combat)
	{
		var ship = status.targetPlayer ? state.ship : combat.otherShip;
		return Instance.IsAffectedByBoost(state, combat, ship, status.status);
	}
	
	private static void RevengeDrive_OnPlayerLoseHull_Prefix(RevengeDrive __instance, out bool __state)
		=> __state = __instance.alreadyActivated;

	private static void RevengeDrive_OnPlayerLoseHull_Postfix(RevengeDrive __instance, State state, Combat combat, ref bool __state)
	{
		if (!__instance.alreadyActivated || __state)
			return;

		// TODO: fix behavior for wrapped actions - this code won't trigger on these, but the original that we're fixing won't either
		if (!Instance.IsAffectedByBoost(state, combat, state.ship, Status.overdrive))
			foreach (var action in combat.cardActions)
				if (action is AAttack { targetPlayer: false, fromDroneX: null } attack)
					attack.damage -= 1 + state.ship.Get(Status.boost);
	}
}

public sealed class VanillaBoostStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static VanillaBoostStatusLogicHook Instance { get; private set; } = new();

	private VanillaBoostStatusLogicHook() { }

	public bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status)
		=> status is Status.shield or Status.tempShield ? false : null;
}

public sealed class VanillaTimestopStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static VanillaTimestopStatusLogicHook Instance { get; private set; } = new();

	private VanillaTimestopStatusLogicHook() { }

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
		=> status != Status.timeStop && amount != 0 && DB.statuses.TryGetValue(status, out var definition) && definition.affectedByTimestop && ship.Get(Status.timeStop) > 0;
}

public sealed class VanillaTurnStartStatusAutoStepLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static VanillaTurnStartStatusAutoStepLogicHook Instance { get; private set; } = new();

	private VanillaTurnStartStatusAutoStepLogicHook() { }

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (timing != StatusTurnTriggerTiming.TurnStart)
			return false;
		if (amount == 0)
			return false;

		var shouldDecrement = status is Status.timeStop or Status.perfectShield;
		if (shouldDecrement)
		{
			amount -= Math.Sign(amount);
			return false;
		}

		var shouldZeroOut = status is Status.stunCharge or Status.tempShield or Status.tempPayback or Status.autododgeLeft or Status.autododgeRight;
		if (shouldZeroOut)
		{
			amount = 0;
			return false;
		}

		return false;
	}
}

public sealed class VanillaTurnEndStatusAutoStepLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static VanillaTurnEndStatusAutoStepLogicHook Instance { get; private set; } = new();

	private VanillaTurnEndStatusAutoStepLogicHook() { }

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return false;
		if (amount == 0)
			return false;

		var shouldDecrement = status is Status.overdrive or Status.temporaryCheap or Status.libra or Status.lockdown or Status.backwardsMissiles;
		if (shouldDecrement)
		{
			amount -= Math.Sign(amount);
			return false;
		}

		var shouldZeroOut = status is Status.autopilot or Status.hermes or Status.engineStall;
		if (shouldZeroOut)
		{
			amount = 0;
			return false;
		}

		return false;
	}
}