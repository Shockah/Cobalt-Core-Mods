using HarmonyLib;
using Microsoft.Extensions.Logging;
using Shockah.Shared;
using System;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class BooksPeriArtifact : DuoArtifact
{
	private const int AttackBuff = 1;
	private const int BuffCost = 1;

	private static bool IsDuringTryPlayCard = false;
	private static int ModifyBaseDamageNestingCounter = 0;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Finalizer))
		);
	}

	private static void Combat_TryPlayCard_Prefix()
		=> IsDuringTryPlayCard = true;

	private static void Combat_TryPlayCard_Finalizer()
		=> IsDuringTryPlayCard = false;

	public override int ModifyBaseDamage(int baseDamage, Card? card, State state, Combat? combat, bool fromPlayer)
	{
		if (!fromPlayer || card is null || combat is null)
			return base.ModifyBaseDamage(baseDamage, card, state, combat, fromPlayer);
		if (!IsDuringTryPlayCard)
			return base.ModifyBaseDamage(baseDamage, card, state, combat, fromPlayer);

		if (ModifyBaseDamageNestingCounter > 0)
			return base.ModifyBaseDamage(baseDamage, card, state, combat, fromPlayer);
		ModifyBaseDamageNestingCounter++;

		var totalShardCost = card.GetActionsOverridden(state, combat)
			.Select(a => a.shardcost)
			.WhereNotNull()
			.Sum();
		ModifyBaseDamageNestingCounter--;

		int shards = state.ship.Get(Status.shard);
		int shield = 0;
		if (state.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			shield = state.ship.Get(Status.shield);

		if (shards + shield < totalShardCost + BuffCost)
			return base.ModifyBaseDamage(baseDamage, card, state, combat, fromPlayer);

		Pulse();
		int leftToPay = BuffCost;

		int shardsToPay = Math.Min(shards, leftToPay);
		if (shardsToPay > 0)
		{
			combat.QueueImmediate(new AStatus
			{
				status = Status.shard,
				statusAmount = -shardsToPay,
				targetPlayer = true
			});
			leftToPay -= shardsToPay;
		}

		int shieldToPay = Math.Min(shield, leftToPay);
		if (shieldToPay > 0)
		{
			combat.QueueImmediate(new AStatus
			{
				status = Status.shard,
				statusAmount = -shieldToPay,
				targetPlayer = true
			});
			leftToPay -= shieldToPay;
		}

		if (leftToPay > 0)
			Instance.Logger!.LogError("Invalid state in {Type}: leftToPay = {LeftToPay}, should be 0", typeof(BooksPeriArtifact), leftToPay);

		return AttackBuff;
	}
}