using Shockah.Kokoro;
using System;
using System.Linq;

namespace Shockah.Dracula;

internal sealed class BleedingManager : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public BleedingManager()
	{
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(this);
		
		ModEntry.Instance.KokoroApi.ActionCosts.RegisterStatusResourceCostIcon(
			ModEntry.Instance.BleedingStatus.Status,
			ModEntry.Instance.BleedingCostOn.Sprite,
			ModEntry.Instance.BleedingCostOff.Sprite
		);
	}

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
	{
		if (args.Status != ModEntry.Instance.BleedingStatus.Status)
			return;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return;
		if (args.OldAmount <= 0)
			return;

		var thinBloodArtifact = args.State.EnumerateAllArtifacts().FirstOrDefault(a => a is ThinBloodArtifact);
		var triggers = thinBloodArtifact is null ? 1 : Math.Min(2, args.OldAmount);

		args.Combat.QueueImmediate(Enumerable.Range(0, triggers).Select(i => new AHurt
		{
			targetPlayer = args.Ship.isPlayerShip,
			hurtAmount = 1,
			hurtShieldsFirst = true,
			artifactPulse = i == 1 ? thinBloodArtifact?.Key() : null
		}));
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Status != ModEntry.Instance.BleedingStatus.Status)
			return false;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;

		var triggers = args.State.EnumerateAllArtifacts().Any(a => a is ThinBloodArtifact) ? 2 : 1;
		if (args.Amount > 0)
			args.Amount = Math.Max(args.Amount - triggers, 0);
		return false;
	}

	public bool CanHandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.ICanHandleImmediateStatusTriggerArgs args)
		=> args.Status == ModEntry.Instance.BleedingStatus.Status && args.Amount > 0;

	public void HandleImmediateStatusTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleImmediateStatusTriggerArgs args)
	{
		var thinBloodArtifact = args.State.EnumerateAllArtifacts().FirstOrDefault(a => a is ThinBloodArtifact);
		var triggers = thinBloodArtifact is null ? 1 : Math.Min(2, args.OldAmount);
		args.NewAmount = Math.Max(args.OldAmount - triggers, 0);
		
		args.Combat.QueueImmediate(Enumerable.Range(0, triggers).Select(i => new AHurt
		{
			targetPlayer = args.Ship.isPlayerShip,
			hurtAmount = 1,
			hurtShieldsFirst = true,
			artifactPulse = i == 1 ? thinBloodArtifact?.Key() : null
		}));
	}
}
