namespace Shockah.DuoArtifacts;

internal sealed class IsaacRiggsArtifact : DuoArtifact, IEvadeHook, IDroneShiftHook, IHookPriority
{
	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 1)
			combat.QueueImmediate(new AStatus
			{
				status = Status.evade,
				statusAmount = 1,
				targetPlayer = true,
				artifactPulse = Key()
			});
	}

	public double HookPriority
		=> -100;

	public bool? IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> state.ship.Get(Status.droneShift) > 0 ? true : null;

	public void PayForEvade(State state, Combat combat, int direction)
	{
		combat.QueueImmediate(new AStatus
		{
			status = Status.droneShift,
			statusAmount = -1,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}

	public bool? IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> state.ship.Get(Status.evade) > 0 ? true : null;

	public void PayForDroneShift(State state, Combat combat, int direction)
	{
		combat.QueueImmediate(new AStatus
		{
			status = Status.evade,
			statusAmount = -1,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}
}