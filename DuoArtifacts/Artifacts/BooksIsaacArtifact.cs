using System;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nickel;

namespace Shockah.DuoArtifacts;

internal sealed class BooksIsaacArtifact : DuoArtifact
{
	private const int AttackBuff = 1;
	private const int BuffCost = 2;

	public bool IsPaidForAndActive;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AttackDrone), "AttackDamage"),
			postfix: new HarmonyMethod(GetType(), nameof(AttackDrone_AttackDamage_Postfix))
		);
	}

	public override void OnTurnStart(State state, Combat combat)
	{
		base.OnTurnStart(state, combat);
		IsPaidForAndActive = false;
	}

	public override void OnTurnEnd(State state, Combat combat)
	{
		base.OnTurnEnd(state, combat);
		if (!combat.stuff.Values.Any(o => o is AttackDrone && !o.targetPlayer))
			return;

		var shards = state.ship.Get(Status.shard);
		var shield = 0;

		var booksDizzyArtifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact);
		if (booksDizzyArtifact is not null)
			shield = state.ship.Get(Status.shield);

		if (shards + shield < BuffCost)
			return;

		var leftToPay = BuffCost;
		var shouldStillPulse = true;

		var shardsToPay = Math.Min(shards, leftToPay);
		if (shardsToPay > 0)
		{
			combat.QueueImmediate(new AStatus
			{
				status = Status.shard,
				statusAmount = -shardsToPay,
				targetPlayer = true,
				artifactPulse = shouldStillPulse ? Key() : null,
			});
			leftToPay -= shardsToPay;
			shouldStillPulse = false;
		}

		var shieldToPay = Math.Min(shield, leftToPay);
		if (shieldToPay > 0)
		{
			booksDizzyArtifact?.Pulse();
			combat.QueueImmediate(new AStatus
			{
				status = Status.shield,
				statusAmount = -shieldToPay,
				targetPlayer = true,
				artifactPulse = shouldStillPulse ? Key() : null,
			});
			leftToPay -= shieldToPay;
			shouldStillPulse = false;
		}

		if (leftToPay > 0)
			Instance.Logger!.LogError("Invalid state in {Type}: leftToPay = {LeftToPay}, should be 0", typeof(BooksIsaacArtifact), leftToPay);

		IsPaidForAndActive = true;
	}

	private static void AttackDrone_AttackDamage_Postfix(AttackDrone __instance, ref int __result)
	{
		if (MG.inst.g?.state is not { } state)
			return;
		if (__instance.targetPlayer)
			return;

		if (state.EnumerateAllArtifacts().OfType<BooksIsaacArtifact>().FirstOrDefault() is not { } artifact)
			return;
		if (!artifact.IsPaidForAndActive)
			return;

		__result += AttackBuff;
	}
}