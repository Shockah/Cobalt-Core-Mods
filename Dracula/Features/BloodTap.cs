using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

internal static class BloodTapExt
{
	public static HashSet<Status> GetBloodTapPlayerStatuses(this Combat self)
		=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<Status>>(self, "BloodTapPlayerStatuses");

	public static HashSet<Status> GetBloodTapEnemyStatuses(this Combat self)
		=> ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<Status>>(self, "BloodTapEnemyStatuses");
}

internal sealed class BloodTapManager : IStatusLogicHook
{
	private readonly Dictionary<Status, Func<State, Combat, Status, List<CardAction>>> Statuses = [];

	public BloodTapManager()
	{
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_Update_Postfix))
		);

		RegisterStatus(Status.evade, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterStatus(Status.droneShift, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterStatus(Status.hermes, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
		]);
		RegisterStatus(Status.payback, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
		RegisterStatus(Status.tempPayback, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterStatus(Status.mitosis, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 2 },
		]);
		RegisterStatus(Status.stunCharge, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterStatus(Status.stunSource, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 2 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 1 },
		]);
		RegisterStatus(Status.serenity, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterStatus(Status.ace, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 2 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
		]);
		RegisterStatus(Status.strafe, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 2 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
		]);
		RegisterStatus(Status.libra, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
		]);
		RegisterStatus(Status.overdrive, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
		]);
		RegisterStatus(Status.powerdrive, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
		RegisterStatus(Status.endlessMagazine, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
		RegisterStatus(Status.autododgeRight, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterStatus(Status.autododgeLeft, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 2 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
		RegisterStatus(Status.autopilot, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AEnergy { changeAmount = 1 }
		]);
		RegisterStatus(Status.boost, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
		]);
		RegisterStatus(Status.temporaryCheap, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterStatus(Status.timeStop, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
		]);
		RegisterStatus(Status.shard, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterStatus(Status.maxShard, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 1 },
		]);
		RegisterStatus(Status.quarry, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
	}

	public void RegisterStatus(Status status, Func<State, Combat, Status, List<CardAction>> actions)
		=> Statuses[status] = actions;

	public List<Status> GetStatuses(Combat combat, bool includeEnemy)
	{
		IEnumerable<Status> allStatuses = combat.GetBloodTapPlayerStatuses();
		if (includeEnemy)
			allStatuses = allStatuses.Concat(combat.GetBloodTapEnemyStatuses()).Distinct();
		return allStatuses.Where(Statuses.ContainsKey).ToList();
	}

	public List<List<CardAction>> MakeChoices(State state, Combat combat, bool includeEnemy)
		=> GetStatuses(combat, includeEnemy)
			.Select(s => (Status: s, Actions: Statuses[s]))
			.Select(e => e.Actions(state, combat, e.Status))
			.ToList();

	private static void UpdateStatuses(HashSet<Status> statuses, Ship ship)
	{
		foreach (var (status, amount) in ship.statusEffects)
		{
			if (amount <= 0)
				continue;
			if (status is Status.shield or Status.tempShield)
				continue;
			statuses.Add(status);
		}
	}

	private static void Combat_Update_Postfix(Combat __instance, G g)
	{
		UpdateStatuses(__instance.GetBloodTapPlayerStatuses(), g.state.ship);
		UpdateStatuses(__instance.GetBloodTapEnemyStatuses(), __instance.otherShip);
	}
}
