using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DrakePeriArtifact : DuoArtifact
{
	private static int? LastOverdrive;

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		LastOverdrive = null;
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		LastOverdrive = state.ship.Get(Status.overdrive);
	}

	public override void AfterPlayerOverheat(State state, Combat combat)
	{
		base.AfterPlayerOverheat(state, combat);
		var artifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is DrakePeriArtifact);
		if (artifact is null)
			return;

		int toConvert = LastOverdrive ?? state.ship.Get(Status.overdrive);
		if (toConvert <= 0)
			return;

		artifact.Pulse();
		combat.QueueImmediate(new AStatus
		{
			status = Status.powerdrive,
			statusAmount = toConvert,
			targetPlayer = true
		});
		state.ship.Set(Status.overdrive, 0);
	}
}
