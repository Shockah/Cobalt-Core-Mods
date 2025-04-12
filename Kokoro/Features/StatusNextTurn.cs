using System.Collections.Generic;
using CobaltCoreModding.Definitions.ExternalItems;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	public ExternalStatus TempShieldNextTurnStatus
		=> Instance.Content.TempShieldNextTurnStatus;

	public Status TempShieldNextTurnVanillaStatus
		=> (Status)TempShieldNextTurnStatus.Id!.Value;

	public ExternalStatus ShieldNextTurnStatus
		=> Instance.Content.ShieldNextTurnStatus;

	public Status ShieldNextTurnVanillaStatus
		=> (Status)ShieldNextTurnStatus.Id!.Value;
	
	#endregion

	partial class V2Api
	{
		public IKokoroApi.IV2.IStatusNextTurnApi StatusNextTurn { get; } = new StatusNextTurnApi();
		
		public sealed class StatusNextTurnApi : IKokoroApi.IV2.IStatusNextTurnApi
		{
			public Status Shield
				=> (Status)ModEntry.Instance.Content.ShieldNextTurnStatus.Id!.Value;
			
			public Status TempShield
				=> (Status)ModEntry.Instance.Content.TempShieldNextTurnStatus.Id!.Value;
			
			public Status Overdrive
				=> (Status)ModEntry.Instance.Content.OverdriveNextTurnStatus.Id!.Value;
		}
	}
}

internal sealed class StatusNextTurnManager : IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static readonly StatusNextTurnManager Instance = new();

	private StatusNextTurnManager()
	{
	}
	
	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		if (args.Status == (Status)ModEntry.Instance.Content.TempShieldNextTurnStatus.Id!.Value)
			return [
				.. args.Tooltips,
				.. StatusMeta.GetTooltips(Status.tempShield, args.Amount)
			];
		if (args.Status == (Status)ModEntry.Instance.Content.ShieldNextTurnStatus.Id!.Value)
			return [
				.. args.Tooltips,
				.. StatusMeta.GetTooltips(Status.shield, args.Amount)
			];
		if (args.Status == (Status)ModEntry.Instance.Content.OverdriveNextTurnStatus.Id!.Value)
			return [
				.. args.Tooltips,
				.. StatusMeta.GetTooltips(Status.overdrive, args.Amount)
			];
		return args.Tooltips;
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;

		if (args.Status == (Status)ModEntry.Instance.Content.TempShieldNextTurnStatus.Id!.Value)
			args.Amount = 0;
		else if (args.Status == (Status)ModEntry.Instance.Content.ShieldNextTurnStatus.Id!.Value)
			args.Amount = 0;
		else if (args.Status == (Status)ModEntry.Instance.Content.OverdriveNextTurnStatus.Id!.Value)
			args.Amount = 0;
		return false;
	}

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return;
		
		if (args.Status == (Status)ModEntry.Instance.Content.TempShieldNextTurnStatus.Id!.Value)
		{
			if (args.OldAmount != 0)
				args.Combat.QueueImmediate(new AStatus
				{
					targetPlayer = args.Ship.isPlayerShip,
					status = Status.tempShield,
					statusAmount = args.OldAmount
				});
		}
		else if (args.Status == (Status)ModEntry.Instance.Content.ShieldNextTurnStatus.Id!.Value)
		{
			if (args.OldAmount != 0)
				args.Combat.QueueImmediate(new AStatus
				{
					targetPlayer = args.Ship.isPlayerShip,
					status = Status.shield,
					statusAmount = args.OldAmount
				});
		}
		else if (args.Status == (Status)ModEntry.Instance.Content.OverdriveNextTurnStatus.Id!.Value)
		{
			if (args.OldAmount != 0)
				args.Combat.QueueImmediate(new AStatus
				{
					targetPlayer = args.Ship.isPlayerShip,
					status = Status.overdrive,
					statusAmount = args.OldAmount
				});
		}
	}
}
