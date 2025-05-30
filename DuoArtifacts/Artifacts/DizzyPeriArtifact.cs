using System;
using Shockah.Kokoro;

namespace Shockah.DuoArtifacts;

public sealed class DizzyPeriArtifact : DuoArtifact, IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IHookPriority
{
	public double HookPriority
		=> double.MinValue;

	public int ModifyStatusChange(IKokoroApi.IV2.IStatusLogicApi.IHook.IModifyStatusChangeArgs args)
	{
		if (!args.Ship.isPlayerShip)
			return args.NewAmount;
		if (args.Status != Status.shield)
			return args.NewAmount;

		var maxShield = args.Ship.GetMaxShield();
		var overshield = Math.Max(0, args.NewAmount - maxShield);
		if (overshield <= 0)
			return args.NewAmount;

		var newAmount = args.NewAmount - overshield;
		args.Combat.QueueImmediate(new AStatus
		{
			status = Status.overdrive,
			statusAmount = 1,
			targetPlayer = true,
			artifactPulse = Key(),
		});
		return newAmount;
	}
}
