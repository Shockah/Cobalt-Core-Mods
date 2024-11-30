namespace Shockah.DuoArtifacts;

internal sealed class BooksRiggsArtifact : DuoArtifact
{
	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		if (combat.turn == 0)
			return;

		var toAdd = state.ship.Get(Status.shard) / 3;
		if (toAdd <= 0)
			return;

		Pulse();
		combat.QueueImmediate(new AStatus
		{
			status = Status.hermes,
			statusAmount = toAdd,
			targetPlayer = true
		});
	}
}