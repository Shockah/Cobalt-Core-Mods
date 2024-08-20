using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class StatusLogicManager : HookManager<IStatusLogicHook>
{
	private static readonly Dictionary<Status, StatusTurnAutoStepSetStrategy> AutoStepSetStrategies = new()
	{
		{ Status.stunCharge, StatusTurnAutoStepSetStrategy.Direct },
		{ Status.tempShield, StatusTurnAutoStepSetStrategy.Direct },
		{ Status.tempPayback, StatusTurnAutoStepSetStrategy.Direct },
		{ Status.autododgeLeft, StatusTurnAutoStepSetStrategy.Direct },
		{ Status.autododgeRight, StatusTurnAutoStepSetStrategy.Direct },
		{ Status.autopilot, StatusTurnAutoStepSetStrategy.Direct },
		{ Status.hermes, StatusTurnAutoStepSetStrategy.Direct },
		{ Status.engineStall, StatusTurnAutoStepSetStrategy.Direct },
		{ Status.lockdown, StatusTurnAutoStepSetStrategy.QueueAdd },
	};

	internal StatusLogicManager()
	{
		Register(VanillaBoostStatusLogicHook.Instance, 0);
		Register(VanillaTimestopStatusLogicHook.Instance, 1_000_000);
		Register(VanillaTurnStartStatusAutoStepLogicHook.Instance, 10);
		Register(VanillaTurnEndStatusAutoStepLogicHook.Instance, 10);
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
		=> OnTurnStartOrEnd(state, combat, StatusTurnTriggerTiming.TurnStart, ship);

	internal void OnTurnEnd(State state, Combat combat, Ship ship)
		=> OnTurnStartOrEnd(state, combat, StatusTurnTriggerTiming.TurnEnd, ship);

	private void OnTurnStartOrEnd(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship)
	{
		var oldAmounts = DB.statuses.Keys
			.ToDictionary(status => status, ship.Get);

		foreach (var (status, oldAmount) in oldAmounts)
		{
			var newAmount = oldAmount;
			var setStrategy = AutoStepSetStrategies.GetValueOrDefault(status, StatusTurnAutoStepSetStrategy.QueueImmediateAdd);

			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
				if (hook.HandleStatusTurnAutoStep(state, combat, timing, ship, status, ref newAmount, ref setStrategy))
					break;
			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, state.EnumerateAllArtifacts()))
				hook.OnStatusTurnTrigger(state, combat, timing, ship, status, oldAmount, newAmount);

			if (newAmount == oldAmount)
				continue;

			switch (setStrategy)
			{
				case StatusTurnAutoStepSetStrategy.Direct:
					ship.Set(status, newAmount);
					break;
				case StatusTurnAutoStepSetStrategy.QueueSet:
					combat.Queue(new AStatus
					{
						targetPlayer = ship.isPlayerShip,
						status = status,
						mode = AStatusMode.Set,
						statusAmount = newAmount
					});
					break;
				case StatusTurnAutoStepSetStrategy.QueueAdd:
					combat.Queue(new AStatus
					{
						targetPlayer = ship.isPlayerShip,
						status = status,
						mode = AStatusMode.Add,
						statusAmount = newAmount - oldAmount
					});
					break;
				case StatusTurnAutoStepSetStrategy.QueueImmediateSet:
					combat.QueueImmediate(new AStatus
					{
						targetPlayer = ship.isPlayerShip,
						status = status,
						mode = AStatusMode.Set,
						statusAmount = newAmount
					});
					break;
				case StatusTurnAutoStepSetStrategy.QueueImmediateAdd:
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
}

public sealed class VanillaBoostStatusLogicHook : IStatusLogicHook
{
	public static VanillaBoostStatusLogicHook Instance { get; private set; } = new();

	private VanillaBoostStatusLogicHook() { }

	public bool? IsAffectedByBoost(State state, Combat combat, Ship ship, Status status)
		=> status is Status.shield or Status.tempShield ? false : null;
}

public sealed class VanillaTimestopStatusLogicHook : IStatusLogicHook
{
	public static VanillaTimestopStatusLogicHook Instance { get; private set; } = new();

	private VanillaTimestopStatusLogicHook() { }

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
		=> status != Status.timeStop && amount != 0 && DB.statuses.TryGetValue(status, out var definition) && definition.affectedByTimestop && ship.Get(Status.timeStop) > 0;
}

public sealed class VanillaTurnStartStatusAutoStepLogicHook : IStatusLogicHook
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

public sealed class VanillaTurnEndStatusAutoStepLogicHook : IStatusLogicHook
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