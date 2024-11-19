using Shockah.Kokoro;

namespace Shockah.DuoArtifacts;

internal sealed class IsaacRiggsArtifact : DuoArtifact, IKokoroApi.IV2.IEvadeHookApi.IHook, IKokoroApi.IV2.IDroneShiftHookApi.IHook, IKokoroApi.IV2.IHookPriority
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

	public bool? IsEvadePossible(IKokoroApi.IV2.IEvadeHookApi.IHook.IIsEvadePossibleArgs args)
		=> args.State.ship.Get(Status.droneShift) > 0 ? true : null;

	public void PayForEvade(IKokoroApi.IV2.IEvadeHookApi.IHook.IPayForEvadeArgs args)
	{
		args.Combat.QueueImmediate(new AStatus
		{
			status = Status.droneShift,
			statusAmount = -1,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}

	public bool? IsDroneShiftPossible(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IIsDroneShiftPossibleArgs args)
		=> args.State.ship.Get(Status.evade) > 0 ? true : null;

	public void PayForDroneShift(IKokoroApi.IV2.IDroneShiftHookApi.IHook.IPayForDroneShiftArgs args)
	{
		args.Combat.QueueImmediate(new AStatus
		{
			status = Status.evade,
			statusAmount = -1,
			targetPlayer = true,
			artifactPulse = Key()
		});
	}
}