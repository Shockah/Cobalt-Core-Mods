using CobaltCoreModding.Definitions.ExternalItems;
using Shockah.Shared;
using System.Collections.Generic;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	public ExternalStatus TempShieldNextTurnStatus
		=> Instance.Content.TempShieldNextTurnStatus;

	public Status TempShieldNextTurnVanillaStatus
		=> (Status)TempShieldNextTurnStatus.Id!.Value;

	public ExternalStatus ShieldNextTurnStatus
		=> Instance.Content.ShieldNextTurnStatus;

	public Status ShieldNextTurnVanillaStatus
		=> (Status)ShieldNextTurnStatus.Id!.Value;
}

internal sealed class StatusNextTurnManager : HookManager<IOxidationStatusHook>, IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static readonly StatusNextTurnManager Instance = new();
	
	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		if (status == (Status)ModEntry.Instance.Content.TempShieldNextTurnStatus.Id!.Value)
			return [
				.. tooltips,
				.. StatusMeta.GetTooltips(Status.tempShield, amount)
			];
		if (status == (Status)ModEntry.Instance.Content.ShieldNextTurnStatus.Id!.Value)
			return [
				.. tooltips,
				.. StatusMeta.GetTooltips(Status.shield, amount)
			];
		return tooltips;
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;

		if (status == (Status)ModEntry.Instance.Content.TempShieldNextTurnStatus.Id!.Value)
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
		else if (status == (Status)ModEntry.Instance.Content.ShieldNextTurnStatus.Id!.Value)
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
