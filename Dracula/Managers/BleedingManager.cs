using System;

namespace Shockah.Dracula;

internal sealed class BleedingManager : IStatusLogicHook
{
	public BleedingManager()
	{
		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(this, 0);
	}

	public void OnStatusTurnTrigger(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount)
	{
		if (status != ModEntry.Instance.BleedingStatus.Status)
			return;
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return;
		if (oldAmount <= 0)
			return;

		combat.QueueImmediate(new AHurt
		{
			targetPlayer = ship.isPlayerShip,
			hurtAmount = 1,
			hurtShieldsFirst = true
		});
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (status != ModEntry.Instance.BleedingStatus.Status)
			return false;
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return false;

		amount = Math.Max(amount - 1, 0);
		return false;
	}
}
