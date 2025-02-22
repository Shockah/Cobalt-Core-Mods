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
		if (state.EnumerateAllArtifacts().FirstOrDefault(a => a is DrakePeriArtifact) is not { } artifact)
			return;

		var toConvert = LastOverdrive ?? state.ship.Get(Status.overdrive);
		if (toConvert <= 0)
			return;

		combat.QueueImmediate([
			new AStatus
			{
				status = Status.overdrive,
				mode = AStatusMode.Set,
				statusAmount = 0,
				targetPlayer = true,
				artifactPulse = artifact.Key(),
				timer = 0,
			},
			new AStatus
			{
				status = Status.powerdrive,
				statusAmount = toConvert,
				targetPlayer = true,
			}
		]);
	}
}
