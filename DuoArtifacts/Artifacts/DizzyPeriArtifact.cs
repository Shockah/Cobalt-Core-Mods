using HarmonyLib;
using Shockah.Shared;
using System;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class DizzyPeriArtifact : DuoArtifact
{
	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_Set_Prefix))
		);
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		int toSubtract = Math.Min(0, state.ship.Get(Status.overdrive));
		if (toSubtract <= 0)
			return;

		combat.QueueImmediate(new AStatus
		{
			status = Status.shield,
			statusAmount = -toSubtract,
			targetPlayer = true
		});
	}

	private static void Ship_Set_Prefix(Ship __instance, Status status, ref int n)
	{
		if (status != Status.shield)
			return;

		var artifact = StateExt.Instance?.EnumerateAllArtifacts().FirstOrDefault(a => a is DizzyPeriArtifact);
		if (artifact is null)
			return;

		if (StateExt.Instance?.route is not Combat combat)
			return;

		int maxShield = __instance.GetMaxShield();
		int overshield = Math.Max(0, n - maxShield);
		if (overshield <= 0)
			return;

		n -= overshield;
		combat.QueueImmediate(new AStatus
		{
			status = Status.overdrive,
			statusAmount = overshield,
			targetPlayer = true
		});
		artifact.Pulse();
	}
}
