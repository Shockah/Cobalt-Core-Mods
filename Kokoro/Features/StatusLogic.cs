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

	internal void OnTurnStart(State state, Combat combat, Ship ship)
		=> OnTurnStartOrEnd(state, combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart, ship);

	internal void OnTurnEnd(State state, Combat combat, Ship ship)
		=> OnTurnStartOrEnd(state, combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd, ship);

	private void OnTurnStartOrEnd(State state, Combat combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming timing, Ship ship)
	{
		var oldStatuses = DB.statuses.Keys
			.Select(status => (Status: status, Amount: ship.Get(status)))
			.ToList();
		
		var statusPriorities = oldStatuses
			.ToDictionary(e => e.Status, e =>
			{
				var modifyStatusTurnTriggerPriorityArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.ModifyStatusTurnTriggerPriorityArgs>();
				try
				{
					modifyStatusTurnTriggerPriorityArgs.State = state;
					modifyStatusTurnTriggerPriorityArgs.Combat = combat;
					modifyStatusTurnTriggerPriorityArgs.Timing = timing;
					modifyStatusTurnTriggerPriorityArgs.Ship = ship;
					modifyStatusTurnTriggerPriorityArgs.Status = e.Status;
					modifyStatusTurnTriggerPriorityArgs.Amount = e.Amount;
					modifyStatusTurnTriggerPriorityArgs.Priority = 0;

					foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
						modifyStatusTurnTriggerPriorityArgs.Priority = hook.ModifyStatusTurnTriggerPriority(modifyStatusTurnTriggerPriorityArgs);

					return modifyStatusTurnTriggerPriorityArgs.Priority;
				}
				finally
				{
					ModEntry.Instance.ArgsPool.Return(modifyStatusTurnTriggerPriorityArgs);
				}
			});

		foreach (var (status, oldAmount) in oldStatuses.OrderByDescending(e => statusPriorities.GetValueOrDefault(e.Status)))
		{
			var newAmount = oldAmount;
			var setStrategy = AutoStepSetStrategies.GetValueOrDefault(status, IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy.QueueImmediateAdd);
			
			var handleStatusTurnAutoStepArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.HandleStatusTurnAutoStepArgs>();
			try
			{
				handleStatusTurnAutoStepArgs.State = state;
				handleStatusTurnAutoStepArgs.Combat = combat;
				handleStatusTurnAutoStepArgs.Timing = timing;
				handleStatusTurnAutoStepArgs.Ship = ship;
				handleStatusTurnAutoStepArgs.Status = status;
				handleStatusTurnAutoStepArgs.Amount = newAmount;
				handleStatusTurnAutoStepArgs.SetStrategy = setStrategy;

				foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
					if (hook.HandleStatusTurnAutoStep(handleStatusTurnAutoStepArgs))
						break;
				newAmount = handleStatusTurnAutoStepArgs.Amount;
				setStrategy = handleStatusTurnAutoStepArgs.SetStrategy;

				var onStatusTurnTriggerArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusLogicApi.OnStatusTurnTriggerArgs>();
				try
				{
					onStatusTurnTriggerArgs.State = state;
					onStatusTurnTriggerArgs.Combat = combat;
					onStatusTurnTriggerArgs.Timing = timing;
					onStatusTurnTriggerArgs.Ship = ship;
					onStatusTurnTriggerArgs.Status = status;
					onStatusTurnTriggerArgs.OldAmount = oldAmount;
					onStatusTurnTriggerArgs.NewAmount = newAmount;

					foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
						hook.OnStatusTurnTrigger(onStatusTurnTriggerArgs);

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
				finally
				{
					ModEntry.Instance.ArgsPool.Return(onStatusTurnTriggerArgs);
				}
			}
			finally
			{
				ModEntry.Instance.ArgsPool.Return(handleStatusTurnAutoStepArgs);
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

	public double ModifyStatusTurnTriggerPriority(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusTurnTriggerPriorityArgs args)
		=> args.Status == ModEntry.Instance.Content.OxidationStatus.Status ? -1_000_000 : args.Priority;

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
		=> args.Status != Status.timeStop && args.Amount != 0 && DB.statuses.TryGetValue(args.Status, out var definition) && definition.affectedByTimestop && args.Ship.Get(Status.timeStop) > 0;
}

public sealed class VanillaTurnStartStatusAutoStepLogicHook : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public static VanillaTurnStartStatusAutoStepLogicHook Instance { get; private set; } = new();

	private VanillaTurnStartStatusAutoStepLogicHook() { }

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;
		if (args.Amount == 0)
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
	public static VanillaTurnEndStatusAutoStepLogicHook Instance { get; private set; } = new();

	private VanillaTurnEndStatusAutoStepLogicHook() { }

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;
		if (args.Amount == 0)
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