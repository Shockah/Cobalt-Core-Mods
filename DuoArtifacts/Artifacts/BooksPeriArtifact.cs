using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nickel;
using Shockah.Shared;
using System;
using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class BooksPeriArtifact : DuoArtifact
{
	private const int AttackBuff = 1;
	private const int BuffCost = 1;

	private static bool IsDuringTryPlayCard;
	private static bool? PaidExtraForAttack;
	private static int ModifyBaseDamageNestingCounter;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Combat_TryPlayCard_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_DrainCardActions_Postfix))
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

		return TryToPay() ? AttackBuff : base.ModifyBaseDamage(baseDamage, card, state, combat, fromPlayer);

		bool TryToPay()
		{
			if (PaidExtraForAttack is not null)
			{
				ModifyBaseDamageNestingCounter--;
				return PaidExtraForAttack.Value;
			}

			var totalShardCost = card.GetActionsOverridden(state, combat)
				.Select(a => a.shardcost)
				.WhereNotNull()
				.Sum();
			ModifyBaseDamageNestingCounter--;

			var shards = state.ship.Get(Status.shard);
			var shield = 0;

			var booksDizzyArtifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact);
			if (booksDizzyArtifact is not null)
				shield = state.ship.Get(Status.shield);

			if (shards + shield < totalShardCost + BuffCost)
			{
				PaidExtraForAttack = false;
				return false;
			}

			Pulse();
			var leftToPay = BuffCost;

			var shardsToPay = Math.Min(shards, leftToPay);
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

			var shieldToPay = Math.Min(shield, leftToPay);
			if (shieldToPay > 0)
			{
				booksDizzyArtifact?.Pulse();
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
			PaidExtraForAttack = true;
			return true;
		}
	}

	private static void Combat_DrainCardActions_Postfix(Combat __instance, G g)
	{
		var artifact = g.state.EnumerateAllArtifacts().OfType<BooksPeriArtifact>().FirstOrDefault();
		if (artifact is null)
			return;

		if (__instance.cardActions.Count == 0)
			PaidExtraForAttack = null;
	}
}