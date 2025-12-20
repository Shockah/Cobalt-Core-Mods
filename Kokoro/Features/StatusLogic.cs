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

	public void RegisterStatusLogicHook(IStatusLogicHook hook, double priority)
		=> StatusLogicManager.Instance.Register(hook, priority);

	public void UnregisterStatusLogicHook(IStatusLogicHook hook)
		=> StatusLogicManager.Instance.Unregister(hook);

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

			public bool CanImmediatelyTriggerStatus(State state, Combat combat, bool targetPlayer, Status status)
				=> StatusLogicManager.Instance.CanImmediatelyTriggerStatus(state, combat, targetPlayer, status);

			public bool ImmediatelyTriggerStatus(State state, Combat combat, bool targetPlayer, Status status, bool keepAmount = false)
				=> StatusLogicManager.Instance.ImmediatelyTriggerStatus(state, combat, targetPlayer, status, keepAmount);

			internal sealed class ModifyStatusChangeArgs : IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
				public int OldAmount { get; internal set; }
				public int NewAmount { get; internal set; }
			}
			
			internal sealed class IsAffectedByBoostArgs : IKokoroApi.IV2.IStatusLogicApi.IHook.IIsAffectedByBoostArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
			}
			
			internal sealed class GetStatusesToCallTurnTriggerHooksForArgs : IKokoroApi.IV2.IStatusLogicApi.IHook.IGetStatusesToCallTurnTriggerHooksForArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming Timing { get; internal set; }
				public Ship Ship { get; internal set; } = null!;
				public IReadOnlySet<Status> KnownStatuses { get; internal set; } = null!;
				public IReadOnlySet<Status> NonZeroStatuses { get; internal set; } = null!;
			}
			
			internal sealed class ModifyStatusTurnTriggerPriorityArgs : IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusTurnTriggerPriorityArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming Timing { get; internal set; }
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
				public int Amount { get; internal set; }
				public double Priority { get; internal set; }
			}
			
			internal sealed class OnStatusTurnTriggerArgs : IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming Timing { get; internal set; }
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
				public int OldAmount { get; internal set; }
				public int NewAmount { get; internal set; }
			}
			
			internal sealed class HandleStatusTurnAutoStepArgs : IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming Timing { get; internal set; }
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
				public int Amount { get; set; }
				public IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy SetStrategy { get; set; }
			}
			
			internal sealed class CanHandleImmediateStatusTriggerArgs : IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
				public int Amount { get; set; }
			}
			
			internal sealed class HandleImmediateStatusTriggerArgs : IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
				public bool KeepAmount { get; internal set; }
				public int OldAmount { get; internal set; }
				public int NewAmount { get; set; }
				public IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy SetStrategy { get; set; }
			}
		}
	}
}

internal sealed class StatusLogicManager : VariedApiVersionHookManager<IKokoroApi.IV2.IStatusLogicApi.IHook, IStatusLogicHook>
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

	private StatusLogicManager() : base(ModEntry.Instance.Package.Manifest.UniqueName, new HookMapper<IKokoroApi.IV2.IStatusLogicApi.IHook, IStatusLogicHook>(hook => new V1ToV2StatusLogicHookWrapper(hook)))
	{
		Register(VanillaBoostStatusLogicHook.Instance, 0);
		Register(VanillaTimestopStatusLogicHook.Instance, 1_000_000);
		Register(VanillaTurnStartStatusAutoStepLogicHook.Instance, 10);
		Register(VanillaTurnEndStatusAutoStepLogicHook.Instance, 10);

		Register(CorrodeTriggerStatusLogicHook.Instance, 0);
		Register(AceTriggerStatusLogicHook.Instance, 0);
		Register(StrafeTriggerStatusLogicHook.Instance, 0);
		Register(LoseEvadeNextTurnStatusLogicHook.Instance, 0);
		Register(DrawNextTurnStatusLogicHook.Instance, 0);
		Register(EnergyNextTurnStatusLogicHook.Instance, 0);
		Register(HeatTriggerStatusLogicHook.Instance, 0);
		Register(EndlessMagazineTriggerStatusLogicHook.Instance, 0);
		Register(RockFactoryTriggerStatusLogicHook.Instance, 0);
		Register(MitosisTriggerStatusLogicHook.Instance, 0);
		Register(QuarryTriggerStatusLogicHook.Instance, 0);
		Register(StunSourceTriggerStatusLogicHook.Instance, 0);
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
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.ModifyStatusChangeArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Ship = ship;
			args.Status = status;
			args.OldAmount = oldAmount;
			args.NewAmount = newAmount;

			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				args.NewAmount = hook.ModifyStatusChange(args);
			return args.NewAmount;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public bool IsAffectedByBoost(State state, Combat combat, Ship ship, Status status)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.IsAffectedByBoostArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Ship = ship;
			args.Status = status;

			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				if (hook.IsAffectedByBoost(args) is { } result)
					return result;
			return true;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public bool CanImmediatelyTriggerStatus(State state, Combat combat, bool targetPlayer, Status status)
	{
		var target = targetPlayer ? state.ship : combat.otherShip;
		
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.CanHandleImmediateStatusTriggerArgs>();
		args.State = state;
		args.Combat = combat;
		args.Ship = target;
		args.Status = status;
		args.Amount = target.Get(status);

		try
		{
			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				if (hook.CanHandleImmediateStatusTrigger(args))
					return true;
			return false;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public bool ImmediatelyTriggerStatus(State state, Combat combat, bool targetPlayer, Status status, bool keepAmount)
	{
		var target = targetPlayer ? state.ship : combat.otherShip;
		
		var canHandleImmediateStatusTriggerArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.CanHandleImmediateStatusTriggerArgs>();
		canHandleImmediateStatusTriggerArgs.State = state;
		canHandleImmediateStatusTriggerArgs.Combat = combat;
		canHandleImmediateStatusTriggerArgs.Ship = target;
		canHandleImmediateStatusTriggerArgs.Status = status;
		canHandleImmediateStatusTriggerArgs.Amount = target.Get(status);
		
		var handleImmediateStatusTriggerArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.HandleImmediateStatusTriggerArgs>();
		handleImmediateStatusTriggerArgs.State = state;
		handleImmediateStatusTriggerArgs.Combat = combat;
		handleImmediateStatusTriggerArgs.Ship = target;
		handleImmediateStatusTriggerArgs.Status = status;
		handleImmediateStatusTriggerArgs.OldAmount = canHandleImmediateStatusTriggerArgs.Amount;
		handleImmediateStatusTriggerArgs.NewAmount = canHandleImmediateStatusTriggerArgs.Amount;
		handleImmediateStatusTriggerArgs.SetStrategy = IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct;
		handleImmediateStatusTriggerArgs.KeepAmount = keepAmount;

		try
		{
			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			{
				if (!hook.CanHandleImmediateStatusTrigger(canHandleImmediateStatusTriggerArgs))
					continue;
				
				target.PulseStatus(status);
				hook.HandleImmediateStatusTrigger(handleImmediateStatusTriggerArgs);

				if (keepAmount)
					return true;
				if (handleImmediateStatusTriggerArgs.NewAmount == handleImmediateStatusTriggerArgs.OldAmount)
					return true;
				if (handleImmediateStatusTriggerArgs.OldAmount <= 0 && handleImmediateStatusTriggerArgs.NewAmount < 0 && !target.CanBeNegative(status))
					return true;

				switch (handleImmediateStatusTriggerArgs.SetStrategy)
				{
					case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.Direct:
						target.Set(status, handleImmediateStatusTriggerArgs.NewAmount);
						break;
					case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueSet:
						combat.Queue(new AStatus
						{
							targetPlayer = target.isPlayerShip,
							status = status,
							mode = AStatusMode.Set,
							statusAmount = handleImmediateStatusTriggerArgs.NewAmount,
						});
						break;
					case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueAdd:
						combat.Queue(new AStatus
						{
							targetPlayer = target.isPlayerShip,
							status = status,
							mode = AStatusMode.Add,
							statusAmount = handleImmediateStatusTriggerArgs.NewAmount - handleImmediateStatusTriggerArgs.OldAmount,
						});
						break;
					case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueImmediateSet:
						combat.QueueImmediate(new AStatus
						{
							targetPlayer = target.isPlayerShip,
							status = status,
							mode = AStatusMode.Set,
							statusAmount = handleImmediateStatusTriggerArgs.NewAmount,
						});
						break;
					case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueImmediateAdd:
						combat.QueueImmediate(new AStatus
						{
							targetPlayer = target.isPlayerShip,
							status = status,
							mode = AStatusMode.Add,
							statusAmount = handleImmediateStatusTriggerArgs.NewAmount - handleImmediateStatusTriggerArgs.OldAmount,
						});
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				
				return true;
			}
			return false;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(canHandleImmediateStatusTriggerArgs);
			ModEntry.Instance.ArgsPool.Return(handleImmediateStatusTriggerArgs);
		}
	}

	internal void OnTurnStart(State state, Combat combat, Ship ship)
		=> OnTurnStartOrEnd(state, combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart, ship);

	internal void OnTurnEnd(State state, Combat combat, Ship ship)
		=> OnTurnStartOrEnd(state, combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd, ship);

	private void OnTurnStartOrEnd(State state, Combat combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming timing, Ship ship)
	{
		var knownStatuses = DB.statuses.Keys.ToHashSet();
		var nonZeroStatuses = ship.statusEffects
			.Where(kvp => kvp.Value != 0)
			.Select(kvp => kvp.Key)
			.ToHashSet();
		
		var getStatusesToCallTurnTriggerHooksForArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.GetStatusesToCallTurnTriggerHooksForArgs>();
		getStatusesToCallTurnTriggerHooksForArgs.State = state;
		getStatusesToCallTurnTriggerHooksForArgs.Combat = combat;
		getStatusesToCallTurnTriggerHooksForArgs.Timing = timing;
		getStatusesToCallTurnTriggerHooksForArgs.Ship = ship;
		getStatusesToCallTurnTriggerHooksForArgs.KnownStatuses = knownStatuses;
		getStatusesToCallTurnTriggerHooksForArgs.NonZeroStatuses = nonZeroStatuses;
		
		var modifyStatusTurnTriggerPriorityArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.ModifyStatusTurnTriggerPriorityArgs>();
		modifyStatusTurnTriggerPriorityArgs.State = state;
		modifyStatusTurnTriggerPriorityArgs.Combat = combat;
		modifyStatusTurnTriggerPriorityArgs.Timing = timing;
		modifyStatusTurnTriggerPriorityArgs.Ship = ship;
		
		var handleStatusTurnAutoStepArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.HandleStatusTurnAutoStepArgs>();
		handleStatusTurnAutoStepArgs.State = state;
		handleStatusTurnAutoStepArgs.Combat = combat;
		handleStatusTurnAutoStepArgs.Timing = timing;
		handleStatusTurnAutoStepArgs.Ship = ship;
		
		var onStatusTurnTriggerArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.OnStatusTurnTriggerArgs>();
		onStatusTurnTriggerArgs.State = state;
		onStatusTurnTriggerArgs.Combat = combat;
		onStatusTurnTriggerArgs.Timing = timing;
		onStatusTurnTriggerArgs.Ship = ship;
		
		try
		{
			var hooks = GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts())
				.Select(hook => (Hook: hook, StatusesToCallHooksFor: hook.GetStatusesToCallTurnTriggerHooksFor(getStatusesToCallTurnTriggerHooksForArgs)))
				.ToList();
			
			var oldStatuses = DB.statuses.Keys
				.Select(status => (Status: status, Amount: ship.Get(status)))
				.ToList();
			
			var statusPriorities = oldStatuses
				.ToDictionary(e => e.Status, e =>
				{
					modifyStatusTurnTriggerPriorityArgs.Status = e.Status;
					modifyStatusTurnTriggerPriorityArgs.Amount = e.Amount;
					modifyStatusTurnTriggerPriorityArgs.Priority = 0;

					foreach (var hook in hooks)
						if (hook.StatusesToCallHooksFor.Contains(e.Status))
							modifyStatusTurnTriggerPriorityArgs.Priority = hook.Hook.ModifyStatusTurnTriggerPriority(modifyStatusTurnTriggerPriorityArgs);

					return modifyStatusTurnTriggerPriorityArgs.Priority;
				});
			
			foreach (var (status, oldAmount) in oldStatuses.OrderByDescending(e => statusPriorities.GetValueOrDefault(e.Status)))
			{
				var newAmount = oldAmount;
				var setStrategy = AutoStepSetStrategies.GetValueOrDefault(status, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueImmediateAdd);
				
				handleStatusTurnAutoStepArgs.Status = status;
				handleStatusTurnAutoStepArgs.Amount = newAmount;
				handleStatusTurnAutoStepArgs.SetStrategy = setStrategy;

				foreach (var hook in hooks)
					if (hook.StatusesToCallHooksFor.Contains(status) && hook.Hook.HandleStatusTurnAutoStep(handleStatusTurnAutoStepArgs))
						break;
				newAmount = handleStatusTurnAutoStepArgs.Amount;
				setStrategy = handleStatusTurnAutoStepArgs.SetStrategy;
				
				onStatusTurnTriggerArgs.Status = status;
				onStatusTurnTriggerArgs.OldAmount = oldAmount;
				onStatusTurnTriggerArgs.NewAmount = newAmount;

				foreach (var hook in hooks)
					if (hook.StatusesToCallHooksFor.Contains(status))
						hook.Hook.OnStatusTurnTrigger(onStatusTurnTriggerArgs);

				if (newAmount == oldAmount)
					continue;
				if (oldAmount <= 0 && newAmount < 0 && !ship.CanBeNegative(status))
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
							statusAmount = newAmount,
						});
						break;
					case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueAdd:
						combat.Queue(new AStatus
						{
							targetPlayer = ship.isPlayerShip,
							status = status,
							mode = AStatusMode.Add,
							statusAmount = newAmount - oldAmount,
						});
						break;
					case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueImmediateSet:
						combat.QueueImmediate(new AStatus
						{
							targetPlayer = ship.isPlayerShip,
							status = status,
							mode = AStatusMode.Set,
							statusAmount = newAmount,
						});
						break;
					case IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueImmediateAdd:
						combat.QueueImmediate(new AStatus
						{
							targetPlayer = ship.isPlayerShip,
							status = status,
							mode = AStatusMode.Add,
							statusAmount = newAmount - oldAmount,
						});
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(getStatusesToCallTurnTriggerHooksForArgs);
			ModEntry.Instance.ArgsPool.Return(modifyStatusTurnTriggerPriorityArgs);
			ModEntry.Instance.ArgsPool.Return(handleStatusTurnAutoStepArgs);
			ModEntry.Instance.ArgsPool.Return(onStatusTurnTriggerArgs);
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
			ModEntry.Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Name, ex);
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
			ModEntry.Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Name, ex);
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
			ModEntry.Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Name, ex);
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

internal sealed class V1ToV2StatusLogicHookWrapper(IStatusLogicHook v1) : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public int ModifyStatusChange(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs args)
		=> v1.ModifyStatusChange(args.State, args.Combat, args.Ship, args.Status, args.OldAmount, args.NewAmount); 
		
	public bool? IsAffectedByBoost(IKokoroApi.IV2.IStatusLogicApi.IHook.IIsAffectedByBoostArgs args)
		=> v1.IsAffectedByBoost(args.State, args.Combat, args.Ship, args.Status);

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
		=> v1.OnStatusTurnTrigger(args.State, args.Combat, (StatusTurnTriggerTiming)(int)args.Timing, args.Ship, args.Status, args.OldAmount, args.NewAmount);

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		var v1SetStrategy = (StatusTurnAutoStepSetStrategy)(int)args.SetStrategy;
		var amount = args.Amount;
		var result = v1.HandleStatusTurnAutoStep(args.State, args.Combat, (StatusTurnTriggerTiming)(int)args.Timing, args.Ship, args.Status, ref amount, ref v1SetStrategy);
		args.SetStrategy = (IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy)(int)v1SetStrategy;
		args.Amount = amount;
		return result;
	}
}

public sealed class VanillaBoostStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static VanillaBoostStatusLogicHook Instance { get; private set; } = new();

	private VanillaBoostStatusLogicHook() { }

	public bool? IsAffectedByBoost(IKokoroApi.IV2.IStatusLogicApi.IHook.IIsAffectedByBoostArgs args)
		=> args.Status is Status.shield or Status.tempShield ? false : null;
}

public sealed class VanillaTimestopStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static VanillaTimestopStatusLogicHook Instance { get; private set; } = new();

	private VanillaTimestopStatusLogicHook() { }

	public IReadOnlySet<Status> GetStatusesToCallTurnTriggerHooksFor(IKokoroApi.IV2.IStatusLogicApi.IHook.IGetStatusesToCallTurnTriggerHooksForArgs args)
		=> args.NonZeroStatuses.Where(status => DB.statuses[status].affectedByTimestop).ToHashSet();

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
		=> args.Ship.Get(Status.timeStop) > 0;
}

public sealed class VanillaTurnStartStatusAutoStepLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	private static readonly HashSet<Status> StatusesToCallTurnTriggerHooksFor = [
		Status.timeStop, Status.perfectShield,
		Status.stunCharge, Status.tempShield, Status.tempPayback, Status.autododgeLeft, Status.autododgeRight,
	];
	
	public static VanillaTurnStartStatusAutoStepLogicHook Instance { get; } = new();

	private VanillaTurnStartStatusAutoStepLogicHook() { }

	public IReadOnlySet<Status> GetStatusesToCallTurnTriggerHooksFor(IKokoroApi.IV2.IStatusLogicApi.IHook.IGetStatusesToCallTurnTriggerHooksForArgs args)
		=> StatusesToCallTurnTriggerHooksFor.Intersect(args.NonZeroStatuses).ToHashSet();

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;

		var shouldDecrement = args.Status is Status.timeStop or Status.perfectShield;
		if (shouldDecrement)
		{
			args.Amount -= Math.Sign(args.Amount);
			return false;
		}

		var shouldZeroOut = args.Status is Status.stunCharge or Status.tempShield or Status.tempPayback or Status.autododgeLeft or Status.autododgeRight;
		if (shouldZeroOut)
		{
			args.Amount = 0;
			return false;
		}

		return false;
	}
}

public sealed class VanillaTurnEndStatusAutoStepLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	private static readonly HashSet<Status> StatusesToCallTurnTriggerHooksFor = [
		Status.overdrive, Status.temporaryCheap, Status.libra, Status.lockdown, Status.backwardsMissiles,
		Status.autopilot, Status.hermes, Status.engineStall,
	];
	
	public static VanillaTurnEndStatusAutoStepLogicHook Instance { get; } = new();

	private VanillaTurnEndStatusAutoStepLogicHook() { }

	public IReadOnlySet<Status> GetStatusesToCallTurnTriggerHooksFor(IKokoroApi.IV2.IStatusLogicApi.IHook.IGetStatusesToCallTurnTriggerHooksForArgs args)
		=> StatusesToCallTurnTriggerHooksFor.Intersect(args.NonZeroStatuses).ToHashSet();

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;

		var shouldDecrement = args.Status is Status.overdrive or Status.temporaryCheap or Status.libra or Status.lockdown or Status.backwardsMissiles;
		if (shouldDecrement)
		{
			args.Amount -= Math.Sign(args.Amount);
			return false;
		}

		var shouldZeroOut = args.Status is Status.autopilot or Status.hermes or Status.engineStall;
		if (shouldZeroOut)
		{
			args.Amount = 0;
			return false;
		}

		return false;
	}
}

public sealed class CorrodeTriggerStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static CorrodeTriggerStatusLogicHook Instance { get; } = new();

	private CorrodeTriggerStatusLogicHook() { }
	
	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.corrode && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
		=> args.Combat.QueueImmediate(new ACorrodeDamage { targetPlayer = args.Ship.isPlayerShip });
}

public sealed class AceTriggerStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static AceTriggerStatusLogicHook Instance { get; } = new();

	private AceTriggerStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.ace && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
		=> args.Combat.QueueImmediate(new AStatus { status = Status.evade, statusAmount = args.OldAmount, targetPlayer = args.Ship.isPlayerShip });
}

public sealed class StrafeTriggerStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static StrafeTriggerStatusLogicHook Instance { get; } = new();

	private StrafeTriggerStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> (args.Status == Status.strafe || args.Status == ModEntry.Instance.Content.TempStrafeStatus.Status) && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
		=> args.Combat.QueueImmediate(new AAttack { damage = Card.GetActualDamage(args.State, args.OldAmount), targetPlayer = !args.Ship.isPlayerShip, fast = true });
}

public sealed class LoseEvadeNextTurnStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static LoseEvadeNextTurnStatusLogicHook Instance { get; } = new();

	private LoseEvadeNextTurnStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.loseEvadeNextTurn && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
	{
		args.Ship.Set(Status.evade, 0);
		args.NewAmount = 0;
	}
}

public sealed class DrawNextTurnStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static DrawNextTurnStatusLogicHook Instance { get; } = new();

	private DrawNextTurnStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.drawNextTurn && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
	{
		args.Combat.QueueImmediate(new ADrawCard { count = args.OldAmount });
		args.NewAmount = 0;
	}
}

public sealed class EnergyNextTurnStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static EnergyNextTurnStatusLogicHook Instance { get; } = new();

	private EnergyNextTurnStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status is (Status.energyNextTurn or Status.energyLessNextTurn) && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
	{
		args.Combat.QueueImmediate(new AEnergy { changeAmount = args.OldAmount * (args.Status == Status.energyLessNextTurn ? -1 : 1) });
		args.NewAmount = 0;
	}
}

public sealed class HeatTriggerStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static HeatTriggerStatusLogicHook Instance { get; } = new();

	private HeatTriggerStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.heat && args.Amount >= args.Ship.heatTrigger;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
	{
		args.Combat.QueueImmediate([
			new AOverheat { targetPlayer = args.Ship.isPlayerShip, timer = args.KeepAmount ? 0 : CardAction.TIMER_DEFAULT },
			.. args.KeepAmount ? [new AStatus { targetPlayer = args.Ship.isPlayerShip, mode = AStatusMode.Set, status = args.Status, statusAmount = args.OldAmount }] : Array.Empty<CardAction>()
		]);
	}
}

public sealed class EndlessMagazineTriggerStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static EndlessMagazineTriggerStatusLogicHook Instance { get; } = new();

	private EndlessMagazineTriggerStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.endlessMagazine && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
		=> args.Combat.QueueImmediate(Enumerable.Range(0, args.OldAmount).Select(_ => new AAddCard { card = new ChipShot(), destination = CardDestination.Hand }));
}

public sealed class RockFactoryTriggerStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static RockFactoryTriggerStatusLogicHook Instance { get; } = new();

	private RockFactoryTriggerStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.rockFactory && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
		=> args.Combat.QueueImmediate(Enumerable.Range(0, args.OldAmount).Select(_ => new ASpawn { thing = new Asteroid { yAnimation = 0 } }));
}

public sealed class MitosisTriggerStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static MitosisTriggerStatusLogicHook Instance { get; } = new();

	private MitosisTriggerStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.mitosis && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
	{
		var stacks = Math.Min(args.OldAmount, args.Ship.Get(Status.shield));
		if (stacks <= 0)
			return;

		args.Combat.QueueImmediate([
			new AStatus { status = Status.shield, statusAmount = -stacks, targetPlayer = args.Ship.isPlayerShip },
			new AStatus { status = Status.tempShield, statusAmount = stacks * 2, targetPlayer = args.Ship.isPlayerShip },
		]);
	}
}

public sealed class QuarryTriggerStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static QuarryTriggerStatusLogicHook Instance { get; } = new();

	private QuarryTriggerStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.quarry && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
		=> args.Combat.QueueImmediate(new AStatus { status = Status.shard, statusAmount = args.OldAmount, targetPlayer = args.Ship.isPlayerShip });
}

public sealed class StunSourceTriggerStatusLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static StunSourceTriggerStatusLogicHook Instance { get; } = new();

	private StunSourceTriggerStatusLogicHook() { }

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == Status.stunSource && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
		=> args.Combat.QueueImmediate(new AStatus { status = Status.stunCharge, statusAmount = args.OldAmount, targetPlayer = args.Ship.isPlayerShip });
}