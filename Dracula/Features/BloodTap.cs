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

internal sealed class BloodTapManager
{
	private readonly Dictionary<Status, Func<State, Combat, Status, List<CardAction>>> StatusOptions = [];
	private readonly HookManager<IBloodTapOptionProvider> StatusOptionProviders = [];

	public BloodTapManager()
	{
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_Update_Postfix))
		);

		RegisterOptionProvider(Status.evade, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterOptionProvider(Status.droneShift, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterOptionProvider(Status.hermes, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
		]);
		RegisterOptionProvider(Status.payback, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
		RegisterOptionProvider(Status.tempPayback, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterOptionProvider(Status.mitosis, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 2 },
		]);
		RegisterOptionProvider(Status.stunCharge, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterOptionProvider(Status.stunSource, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 2 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.stunCharge, statusAmount = 1 },
		]);
		RegisterOptionProvider(Status.serenity, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterOptionProvider(Status.ace, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 2 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 1 },
		]);
		RegisterOptionProvider(Status.strafe, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 2 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.evade, statusAmount = 2 },
		]);
		RegisterOptionProvider(Status.libra, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
		]);
		RegisterOptionProvider(Status.overdrive, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
		]);
		RegisterOptionProvider(Status.powerdrive, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
		RegisterOptionProvider(Status.endlessMagazine, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
		RegisterOptionProvider(Status.bubbleJuice, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
		]);
		RegisterOptionProvider(Status.autododgeRight, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterOptionProvider(Status.autododgeLeft, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 2 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
		RegisterOptionProvider(Status.autopilot, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AEnergy { changeAmount = 1 }
		]);
		RegisterOptionProvider(Status.boost, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
		]);
		RegisterOptionProvider(Status.temporaryCheap, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterOptionProvider(Status.timeStop, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
		]);
		RegisterOptionProvider(Status.shard, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
		]);
		RegisterOptionProvider(Status.maxShard, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AStatus { targetPlayer = true, status = Status.shard, statusAmount = 1 },
		]);
		RegisterOptionProvider(Status.quarry, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);
	}

	public void RegisterOptionProvider(Status status, Func<State, Combat, Status, List<CardAction>> actions)
		=> StatusOptions[status] = actions;

	public void RegisterOptionProvider(IBloodTapOptionProvider provider, double priority = 0)
		=> StatusOptionProviders.Register(provider, priority);

	public HashSet<Status> GetAllOwnedStatuses(Combat combat, bool includeEnemy)
	{
		IEnumerable<Status> allStatuses = combat.GetBloodTapPlayerStatuses();
		if (includeEnemy)
			allStatuses = allStatuses.Concat(combat.GetBloodTapEnemyStatuses()).Distinct();
		return allStatuses.ToHashSet();
	}

	public List<Status> GetApplicableStatuses(State state, Combat combat, bool includeEnemy)
	{
		var allOwnedStatues = GetAllOwnedStatuses(combat, includeEnemy);
		var applicableStatuses = allOwnedStatues.Where(StatusOptions.ContainsKey).ToList();
		foreach (var provider in StatusOptionProviders)
			foreach (var providerApplicableStatus in provider.GetBloodTapApplicableStatuses(state, combat, allOwnedStatues))
				if (!applicableStatuses.Contains(providerApplicableStatus))
					applicableStatuses.Add(providerApplicableStatus);
		return applicableStatuses;
	}

	public IEnumerable<List<CardAction>> MakeChoices(State state, Combat combat, bool includeEnemy)
	{
		var allOwnedStatues = GetAllOwnedStatuses(combat, includeEnemy);
		foreach (var (status, provider) in StatusOptions)
		{
			if (!allOwnedStatues.Contains(status))
				continue;
			var actions = provider(state, combat, status);
			if (actions.Count != 0)
				yield return actions;
		}
		foreach (var provider in StatusOptionProviders)
			foreach (var option in provider.GetBloodTapOptionsActions(state, combat, allOwnedStatues))
				yield return option;
	}

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
