using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class StatusNextTurnManager : HookManager<IOxidationStatusHook>, IStatusLogicHook, IStatusRenderHook
{
	private static ModEntry Instance => ModEntry.Instance;

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		if (status == (Status)Instance.Content.TempShieldNextTurnStatus.Id!.Value)
			return [
				.. tooltips,
				.. StatusMeta.GetTooltips(Status.tempShield, amount)
			];
		if (status == (Status)Instance.Content.ShieldNextTurnStatus.Id!.Value)
			return [
				.. tooltips,
				.. StatusMeta.GetTooltips(Status.shield, amount)
			];
		return tooltips;
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (timing != StatusTurnTriggerTiming.TurnStart)
			return false;

		if (status == (Status)Instance.Content.TempShieldNextTurnStatus.Id!.Value)
		{
			if (amount != 0)
				combat.QueueImmediate(new AStatus
				{
					targetPlayer = ship.isPlayerShip,
					status = Status.tempShield,
					statusAmount = amount
				});
			amount = 0;
		}
		else if (status == (Status)Instance.Content.ShieldNextTurnStatus.Id!.Value)
		{
			if (amount != 0)
				combat.QueueImmediate(new AStatus
				{
					targetPlayer = ship.isPlayerShip,
					status = Status.shield,
					statusAmount = amount
				});
			amount = 0;
		}
		return false;
	}
}
