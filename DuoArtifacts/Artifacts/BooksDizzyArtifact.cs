using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.DuoArtifacts;

internal sealed class BooksDizzyArtifact : DuoArtifact, IKokoroApi.IV2.IActionCostsApi.IHook
{
	private static readonly Stack<(int Shield, int MaxShield, int Shards, int MaxShards)> OldStatusStates = new();
	private static int TemporarilyAppliedShieldToShardsCounter;

	public int ShardsToRestoreOnNextCombat;

	protected internal override void ApplyPatches(IHarmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_DrainCardActions_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Combat_DrainCardActions_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ShieldGun), "GetShieldAmt"),
			postfix: new HarmonyMethod(GetType(), nameof(ShieldGun_GetShieldAmt_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Converter), "GetShieldAmt"),
			postfix: new HarmonyMethod(GetType(), nameof(Converter_GetShieldAmt_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Converter), nameof(Converter.GetActions)),
			postfix: new HarmonyMethod(GetType(), nameof(Converter_GetActions_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Inverter), "GetShieldAmt"),
			postfix: new HarmonyMethod(GetType(), nameof(Inverter_GetShieldAmt_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Inverter), nameof(Converter.GetActions)),
			postfix: new HarmonyMethod(GetType(), nameof(Inverter_GetActions_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Glimmershot), nameof(Glimmershot.GetShard)),
			postfix: new HarmonyMethod(GetType(), nameof(Glimmershot_GetShard_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Glimmershot), nameof(Glimmershot.GetActions)),
			postfix: new HarmonyMethod(GetType(), nameof(Glimmershot_GetActions_Postfix))
		);
	}

	public override void OnCombatStart(State state, Combat combat)
	{
		base.OnCombatStart(state, combat);
		if (ShardsToRestoreOnNextCombat <= 0)
			return;

		var shieldMemoryArtifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is ShieldMemory);
		if (shieldMemoryArtifact is null)
		{
			ShardsToRestoreOnNextCombat = 0;
			return;
		}

		Pulse();
		shieldMemoryArtifact.Pulse();
		combat.QueueImmediate(new AStatus
		{
			targetPlayer = true,
			status = Status.shield,
			statusAmount = ShardsToRestoreOnNextCombat
		});
		ShardsToRestoreOnNextCombat = 0;
	}

	public override void OnCombatEnd(State state)
	{
		base.OnCombatEnd(state);
		if (state.EnumerateAllArtifacts().Any(a => a is ShieldMemory))
			ShardsToRestoreOnNextCombat = state.ship.Get(Status.shard);
	}

	private static void AddStatusNoPulse(Ship ship, Status status, int n)
	{
		double? statusEffectPulse = ship.statusEffectPulses.TryGetValue(status, out var dictValue) ? dictValue : null;
		var pendingOneShotStatusAnimation = ship.pendingOneShotStatusAnimations.Contains(status);

		ship.Add(status, n);

		if (statusEffectPulse is null)
			ship.statusEffectPulses.Remove(status);
		else
			ship.statusEffectPulses[status] = statusEffectPulse.Value;

		if (pendingOneShotStatusAnimation)
			ship.pendingOneShotStatusAnimations.Add(status);
		else
			ship.pendingOneShotStatusAnimations.Remove(status);
	}

	private static void PushStatusState(Ship ship)
		=> OldStatusStates.Push((Shield: ship.Get(Status.shield), MaxShield: ship.Get(Status.maxShield), Shards: ship.Get(Status.shard), MaxShards: ship.Get(Status.maxShard)));

	private static (int Shield, int Shards) PopStatusState()
	{
		var (shield, _, shards, _) = OldStatusStates.Pop();
		return (shield, shards);
	}

	private static void Ship_NormalDamage_Prefix(Ship __instance, State s)
	{
		if (__instance != s.ship)
			return;
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;

		var shards = __instance.Get(Status.shard);
		PushStatusState(__instance);
		AddStatusNoPulse(__instance, Status.maxShield, shards);
		AddStatusNoPulse(__instance, Status.shield, shards);
	}

	private static void Ship_NormalDamage_Finalizer(Ship __instance, State s, Combat c)
	{
		if (__instance != s.ship)
			return;
		if (s.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact) is not { } artifact)
			return;

		var (_, oldShards) = PopStatusState();
		var currentShield = __instance.Get(Status.shield);
		var currentShards = __instance.Get(Status.shard);

		var resultingShield = currentShield - oldShards;

		var shardsToRemove = Math.Max(0, -resultingShield);
		resultingShield += shardsToRemove;
		var resultingShards = currentShards - shardsToRemove;

		var extraHullToRemove = Math.Max(0, -resultingShards);
		resultingShards += extraHullToRemove;

		if (resultingShards != currentShards)
		{
			__instance.Add(Status.shard, resultingShards - currentShards);
			artifact.Pulse();
		}
		if (resultingShield != currentShield)
			AddStatusNoPulse(__instance, Status.shield, resultingShield - currentShield);

		AddStatusNoPulse(__instance, Status.maxShield, -oldShards);
		if (extraHullToRemove > 0)
			__instance.DirectHullDamage(s, c, extraHullToRemove);
	}

	private static void Combat_DrainCardActions_Prefix(G g)
	{
		if (TemporarilyAppliedShieldToShardsCounter > 0)
			return;
		if (!g.state.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;

		var shield = g.state.ship.Get(Status.shield);
		PushStatusState(g.state.ship);
		AddStatusNoPulse(g.state.ship, Status.maxShard, shield);
		AddStatusNoPulse(g.state.ship, Status.shard, shield);
		TemporarilyAppliedShieldToShardsCounter++;
	}

	private static void Combat_DrainCardActions_Finalizer(G g)
	{
		if (TemporarilyAppliedShieldToShardsCounter <= 0)
			return;
		if (g.state.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact) is not { } artifact)
			return;

		TemporarilyAppliedShieldToShardsCounter--;
		if (TemporarilyAppliedShieldToShardsCounter > 0)
			return;

		var (oldShield, _) = PopStatusState();
		var currentShards = g.state.ship.Get(Status.shard);
		var currentShield = g.state.ship.Get(Status.shield);

		var resultingShards = currentShards - oldShield;

		var shieldToRemove = Math.Max(0, -resultingShards);
		resultingShards += shieldToRemove;
		var resultingShield = currentShield - shieldToRemove;

		var leftOverToRemove = Math.Max(0, -resultingShield);
		resultingShield += leftOverToRemove;

		if (resultingShield != currentShield)
		{
			AddStatusNoPulse(g.state.ship, Status.shield, resultingShield - currentShield);
			artifact.Pulse();
		}
		if (resultingShards != currentShards)
			AddStatusNoPulse(g.state.ship, Status.shard, resultingShards - currentShards);

		AddStatusNoPulse(g.state.ship, Status.maxShard, -oldShield);
		// honestly not sure what to do about this?
		//if (leftOverToRemove > 0)
		//	g.state.ship.DirectHullDamage(s, c, leftOverToRemove);
	}

	private static void ShieldGun_GetShieldAmt_Postfix(State s, ref int __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result += s.ship.Get(Status.shard);
	}

	private static void Converter_GetShieldAmt_Postfix(State s, ref int __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result += s.ship.Get(Status.shard);
	}

	private static void Converter_GetActions_Postfix(Converter __instance, State s, ref List<CardAction> __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		if (__instance.upgrade == Upgrade.B)
			return;
		__result.Add(Instance.KokoroApi.HiddenActions.MakeAction(new AStatus
		{
			status = Status.shard,
			statusAmount = 0,
			mode = AStatusMode.Set,
			targetPlayer = true,
		}).AsCardAction);
	}

	private static void Inverter_GetShieldAmt_Postfix(State s, ref int __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result += s.ship.Get(Status.shard);
	}

	private static void Inverter_GetActions_Postfix(State s, ref List<CardAction> __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result.Add(Instance.KokoroApi.HiddenActions.MakeAction(new AStatus
		{
			status = Status.shard,
			statusAmount = 0,
			mode = AStatusMode.Set,
			targetPlayer = true,
		}).AsCardAction);
	}

	private static void Glimmershot_GetShard_Postfix(State s, ref int __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result += s.ship.Get(Status.shield);
	}

	private static void Glimmershot_GetActions_Postfix(State s, ref List<CardAction> __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result.Add(Instance.KokoroApi.HiddenActions.MakeAction(new AStatus
		{
			status = Status.shield,
			statusAmount = 0,
			mode = AStatusMode.Set,
			targetPlayer = true,
		}).AsCardAction);
	}
	
	public bool ModifyActionCost(IKokoroApi.IV2.IActionCostsApi.IHook.IModifyActionCostArgs args)
	{
		HandleAnyCost(args.Cost);
		return false;

		void HandleAnyCost(IKokoroApi.IV2.IActionCostsApi.ICost cost)
		{
			if (ModEntry.Instance.KokoroApi.ActionCosts.AsResourceCost(cost) is { } resourceCost)
				HandleResourceCost(resourceCost);
			else if (ModEntry.Instance.KokoroApi.ActionCosts.AsCombinedCost(cost) is { } combinedCost)
				HandleCombinedCost(combinedCost);
		}

		void HandleCombinedCost(IKokoroApi.IV2.IActionCostsApi.ICombinedCost combinedCost)
		{
			foreach (var cost in combinedCost.Costs)
				HandleAnyCost(cost);
		}
		
		void HandleResourceCost(IKokoroApi.IV2.IActionCostsApi.IResourceCost cost)
		{
			if (!cost.PotentialResources.Any(r => ModEntry.Instance.KokoroApi.ActionCosts.AsStatusResource(r) is { Status: Status.shard }))
				return;
			cost.PotentialResources.Add(ModEntry.Instance.KokoroApi.ActionCosts.MakeStatusResource(Status.shield));
		}
	}
}
