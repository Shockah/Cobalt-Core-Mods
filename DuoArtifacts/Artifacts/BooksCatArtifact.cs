namespace Shockah.DuoArtifacts;

internal sealed class BooksCatArtifact : DuoArtifact
{
	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (combat.turn == 0 || combat.energy <= 0)
			return;

		Pulse();
		combat.QueueImmediate(new AStatus
		{
			status = Status.shard,
			statusAmount = combat.energy,
			targetPlayer = true
		});
	}
}