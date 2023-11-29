using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

internal sealed class CatMaxArtifact : DuoArtifact, IEvadeHook, IDroneShiftHook
{
	private static readonly List<Status> PossibleStatuses = new()
	{
		Status.overdrive,
		Status.powerdrive,
		Status.ace,
		Status.perfectShield,
		Status.strafe,
		Status.stunCharge,
		Status.evade,
		Status.autododgeLeft,
		Status.autododgeRight,
		Status.maxShield,
		Status.drawNextTurn,
		Status.energyNextTurn,
		Status.droneShift,
		Status.serenity,
		Status.endlessMagazine,
		Status.rockFactory,
		Status.boost,
		Status.autopilot,
		Status.cleanExhaust,
		Status.mitosis,
		Status.payback,
		Status.hermes,
		Status.reflexiveCoating,
		Status.libra,
		Status.timeStop,
		Status.shard,
		Status.maxShard,
		Status.quarry,
		Status.tableFlip,
		Status.stunSource,
		Status.temporaryCheap,
		Status.bubbleJuice
	};

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 0)
			return;

		combat.QueueImmediate(new AStatus
		{
			status = PossibleStatuses[state.rngActions.NextInt() % PossibleStatuses.Count],
			statusAmount = 1,
			targetPlayer = true
		});
	}
}