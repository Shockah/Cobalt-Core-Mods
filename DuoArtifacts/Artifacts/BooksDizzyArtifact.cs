using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.DuoArtifacts;

internal sealed class BooksDizzyArtifact : DuoArtifact
{
	private static ExternalSprite ShieldCostSprite = null!;

	private static readonly Stack<(int Shield, int MaxShield, int Shards, int MaxShards)> OldStatusStates = new();
	private static int TemporarilyAppliedShieldToShardsCounter;
	private static int RenderActionShardAvailable;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
	[SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Set via IL")]
	private static int ShardCostIconIndex;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

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
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			transpiler: new HarmonyMethod(GetType(), nameof(Card_MakeAllActionIcons_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Prefix)),
			transpiler: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Transpiler))
		);
		harmony.Patch(
			original: typeof(Card).GetMethods(AccessTools.all).First(m => m.Name.StartsWith("<RenderAction>g__ShardcostIcon") && m.ReturnType == typeof(void)),
			transpiler: new HarmonyMethod(GetType(), nameof(Card_RenderAction_ShardcostIcon_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AVariableHint), nameof(AVariableHint.GetTooltips)),
			transpiler: new HarmonyMethod(GetType(), nameof(AVariableHint_GetTooltips_Transpiler))
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

	protected internal override void RegisterArt(ISpriteRegistry registry, string namePrefix, DuoArtifactDefinition definition)
	{
		base.RegisterArt(registry, namePrefix, definition);
		ShieldCostSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Icon.ShieldCost",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Icons", "ShieldCost.png"))
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
		var artifact = s.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact);
		if (artifact is null)
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
		var artifact = g.state.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact);
		if (artifact is null)
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

	private static IEnumerable<CodeInstruction> Card_MakeAllActionIcons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(2),
					ILMatches.Ldfld("ship"),
					ILMatches.LdcI4((int)Status.shard),
					ILMatches.Call("Get")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BooksDizzyArtifact), nameof(Card_MakeAllActionIcons_Transpiler_ModifyStatusValue)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static int Card_MakeAllActionIcons_Transpiler_ModifyStatusValue(int value, State state)
	{
		if (!state.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return value;
		return value + state.ship.Get(Status.shield);
	}

	private static void Card_RenderAction_Prefix(int shardAvailable)
		=> RenderActionShardAvailable = shardAvailable;

	private static IEnumerable<CodeInstruction> Card_RenderAction_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(4).ExtractLabels(out var labels),
					ILMatches.Ldloc<int>(originalMethod).CreateLdlocInstruction(out var ldlocLoopIterator),
					ILMatches.Instruction(OpCodes.Clt),
					ILMatches.LdcI4(0),
					ILMatches.Instruction(OpCodes.Ceq),
					ILMatches.AnyLdloca,
					ILMatches.Instruction(OpCodes.Call)
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.JustInsertion,
					ldlocLoopIterator.Value.WithLabels(labels),
					new CodeInstruction(OpCodes.Stsfld, AccessTools.DeclaredField(typeof(BooksDizzyArtifact), nameof(ShardCostIconIndex)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static IEnumerable<CodeInstruction> Card_RenderAction_ShardcostIcon_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Brtrue,
					ILMatches.LdcI4((int)StableSpr.icons_shardcostoff),
					ILMatches.Br,
					ILMatches.LdcI4((int)StableSpr.icons_shardcost)
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BooksDizzyArtifact), nameof(Card_RenderAction_ShardcostIcon_Transpiler_ModifySprite)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static int Card_RenderAction_ShardcostIcon_Transpiler_ModifySprite(int spriteId, bool costMet)
	{
		if (!costMet)
			return spriteId;
		if (MG.inst.g.state is not { } state || !state.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return spriteId;

		var shield = state.ship.Get(Status.shield);
		var shards = state.ship.Get(Status.shard);

		var totalIndex = ShardCostIconIndex + (shards + shield - RenderActionShardAvailable);
		if (totalIndex > shards)
			return ShieldCostSprite.Id!.Value;
		return spriteId;
	}

	private static IEnumerable<CodeInstruction> AVariableHint_GetTooltips_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(1),
					ILMatches.Ldfld("ship"),
					ILMatches.Ldarg(0),
					ILMatches.Ldflda("status"),
					ILMatches.Call("get_Value"),
					ILMatches.Call("Get")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BooksDizzyArtifact), nameof(AVariableHint_GetTooltips_Transpiler_ModifyStatusValue)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static int AVariableHint_GetTooltips_Transpiler_ModifyStatusValue(int value, AVariableHint hint, State state)
	{
		if (!state.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return value;
		return hint.status switch
		{
			Status.shield => value + state.ship.Get(Status.shard),
			Status.shard => value + state.ship.Get(Status.shield),
			_ => value
		};
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
			targetPlayer = true
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
			targetPlayer = true
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
			targetPlayer = true
		}).AsCardAction);
	}
}
