using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace Shockah.DuoArtifacts;

internal sealed class BooksIsaacArtifact : DuoArtifact
{
	private const int AttackBuff = 1;
	private const int BuffCost = 2;

	public bool IsPaidForAndActive = false;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		// this doesn't work, the method gets inlined; transpile `GetActions` and `GetTooltips` instead
		//harmony.TryPatch(
		//	logger: Instance.Logger!,
		//	original: () => AccessTools.DeclaredMethod(typeof(AttackDrone), "AttackDamage"),
		//	postfix: new HarmonyMethod(GetType(), nameof(AttackDrone_AttackDamage_Postfix))
		//);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(AttackDrone), nameof(AttackDrone.GetActions)),
			transpiler: new HarmonyMethod(GetType(), nameof(AttackDrone_GetActions_Transpiler))
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

		int shards = state.ship.Get(Status.shard);
		int shield = 0;

		var booksDizzyArtifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact);
		if (booksDizzyArtifact is not null)
			shield = state.ship.Get(Status.shield);

		if (shards + shield < BuffCost)
			return;

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
			Instance.Logger!.LogError("Invalid state in {Type}: leftToPay = {LeftToPay}, should be 0", typeof(BooksIsaacArtifact), leftToPay);

		IsPaidForAndActive = true;
	}

	private static int GetModifiedAttackDamage(int damage, AttackDrone drone)
	{
		if (StateExt.Instance is not { } state)
			return damage;
		if (drone.targetPlayer)
			return damage;

		var artifact = state.EnumerateAllArtifacts().OfType<BooksIsaacArtifact>().FirstOrDefault();
		if (artifact is null || !artifact.IsPaidForAndActive)
			return damage;

		artifact.Pulse();
		damage += AttackBuff;
		return damage;
	}

	private static IEnumerable<CodeInstruction> AttackDrone_GetActions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("AttackDamage"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BooksIsaacArtifact), nameof(GetModifiedAttackDamage)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}
}