using System.Linq;

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

	bool? IEvadeHook.IsEvadePossible(State state, Combat combat, EvadeHookContext context)
		=> state.ship.Get(Status.droneShift) > 0;

	void IEvadeHook.PayForEvade(State state, Combat combat, int direction)
	{
		combat.QueueImmediate(new AStatus
		{
			status = Status.droneShift,
			statusAmount = -1,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}

	bool? IDroneShiftHook.IsDroneShiftPossible(State state, Combat combat, DroneShiftHookContext context)
		=> state.EnumerateAllArtifacts().Any(a => a is IsaacRiggsArtifact) && state.ship.Get(Status.evade) > 0;

	void IDroneShiftHook.PayForDroneShift(State state, Combat combat, int direction)
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