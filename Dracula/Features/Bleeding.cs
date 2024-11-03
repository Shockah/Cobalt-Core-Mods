using Shockah.Kokoro;
using System;
using System.Linq;

namespace Shockah.Dracula;

internal sealed class BleedingManager : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public BleedingManager()
	{
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(this);
	}

	public void OnStatusTurnTrigger(State state, Combat combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount)
	{
		if (status != ModEntry.Instance.BleedingStatus.Status)
			return;
		if (timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return;
		if (oldAmount <= 0)
			return;

		var thinBloodArtifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is ThinBloodArtifact);
		var triggers = thinBloodArtifact is null ? 1 : Math.Min(2, oldAmount);

		combat.QueueImmediate(Enumerable.Range(0, triggers).Select(i => new AHurt
		{
			targetPlayer = ship.isPlayerShip,
			hurtAmount = 1,
			hurtShieldsFirst = true,
			artifactPulse = i == 1 ? thinBloodArtifact?.Key() : null
		}));
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref IKokoroApi.IV2.IStatusLogicApi.StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (status != ModEntry.Instance.BleedingStatus.Status)
			return false;
		if (timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;

		var triggers = state.EnumerateAllArtifacts().Any(a => a is ThinBloodArtifact) ? 2 : 1;
		if (amount > 0)
			amount = Math.Max(amount - triggers, 0);
		return false;
	}
}
