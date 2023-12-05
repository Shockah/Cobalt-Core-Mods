namespace Shockah.DuoArtifacts;

internal sealed class MaxRiggsArtifact : DuoArtifact
{
	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 0)
			return;

		Pulse();
		combat.Queue(new AStatus
		{
			status = Status.engineStall,
			statusAmount = 1,
			targetPlayer = true
		});
		combat.Queue(new AStatus
		{
			status = Status.autopilot,
			statusAmount = 1,
			targetPlayer = true
		});
	}
}