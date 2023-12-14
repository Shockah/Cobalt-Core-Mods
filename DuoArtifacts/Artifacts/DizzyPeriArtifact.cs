using System;

namespace Shockah.DuoArtifacts;

public sealed class DizzyPeriArtifact : DuoArtifact, IStatusLogicHook, IHookPriority
{
	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);

		int toSubtract = Math.Clamp(state.ship.Get(Status.overdrive), 0, state.ship.Get(Status.shield));
		if (toSubtract > 0)
			combat.QueueImmediate(new AStatus
			{
				status = Status.shield,
				statusAmount = -toSubtract,
				targetPlayer = true
			});

		toSubtract = Math.Clamp(state.ship.Get(Status.perfectShield), 0, state.ship.Get(Status.overdrive));
		if (toSubtract > 0)
			combat.QueueImmediate(new AStatus
			{
				status = Status.overdrive,
				statusAmount = -toSubtract,
				targetPlayer = true
			});
	}

	public double HookPriority
		=> double.MinValue;

	public int ModifyStatusChange(State state, Combat combat, Ship ship, Status status, int oldAmount, int newAmount)
	{
		if (status != Status.shield)
			return newAmount;

		int maxShield = ship.GetMaxShield();
		int overshield = Math.Max(0, newAmount - maxShield);
		if (overshield <= 0)
			return newAmount;

		newAmount -= overshield;
		combat.QueueImmediate(new AStatus
		{
			status = Status.overdrive,
			statusAmount = overshield,
			targetPlayer = true
		});
		Pulse();
		return newAmount;
	}
}
