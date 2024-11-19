using CobaltCoreModding.Definitions.ExternalItems;
using Shockah.Shared;
using System.Collections.Generic;

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
		}
	}
}

internal sealed class StatusNextTurnManager : HookManager<IOxidationStatusHook>, IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	internal static readonly StatusNextTurnManager Instance = new();

	private StatusNextTurnManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}
	
	public List<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
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
		return args.Tooltips;
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;

		if (args.Status == (Status)ModEntry.Instance.Content.TempShieldNextTurnStatus.Id!.Value)
		{
			if (args.Amount != 0)
				args.Combat.QueueImmediate(new AStatus
				{
					targetPlayer = args.Ship.isPlayerShip,
					status = Status.tempShield,
					statusAmount = args.Amount
				});
			args.Amount = 0;
		}
		else if (args.Status == (Status)ModEntry.Instance.Content.ShieldNextTurnStatus.Id!.Value)
		{
			if (args.Amount != 0)
				args.Combat.QueueImmediate(new AStatus
				{
					targetPlayer = args.Ship.isPlayerShip,
					status = Status.shield,
					statusAmount = args.Amount
				});
			args.Amount = 0;
		}
		return false;
	}
}
