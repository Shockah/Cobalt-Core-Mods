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
using CobaltCoreModding.Definitions.ModContactPoints;
using System.IO;
using CobaltCoreModding.Definitions.ExternalItems;
using System.Diagnostics.CodeAnalysis;

namespace Shockah.DuoArtifacts;

internal sealed class BooksDizzyArtifact : DuoArtifact
{
	private static ExternalSprite ShieldCostSprite = null!;

	private static readonly Stack<(int Shield, int MaxShield, int Shards, int MaxShards)> OldStatusStates = new();
	private static int TemporarilyAppliedShieldToShardsCounter = 0;

	private static int RenderActionShardAvailable;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Set via IL")]
	private static int ShardCostIconIndex;

	protected internal override void ApplyPatches(Harmony harmony)
	{
		base.ApplyPatches(harmony);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.NormalDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Ship_NormalDamage_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrainCardActions)),
			prefix: new HarmonyMethod(GetType(), nameof(Combat_DrainCardActions_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Combat_DrainCardActions_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.MakeAllActionIcons)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_MakeAllActionIcons_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(Card_MakeAllActionIcons_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(CardAction), nameof(CardAction.GetTooltipsForActions)),
			prefix: new HarmonyMethod(GetType(), nameof(CardAction_GetTooltipsForActions_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(CardAction_GetTooltipsForActions_Finalizer))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction)),
			prefix: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Prefix)),
			transpiler: new HarmonyMethod(GetType(), nameof(Card_RenderAction_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => typeof(Card).GetMethods(AccessTools.all).First(m => m.Name.StartsWith("<RenderAction>g__ShardcostIcon") && m.ReturnType == typeof(void)),
			transpiler: new HarmonyMethod(GetType(), nameof(Card_RenderAction_ShardcostIcon_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ShieldGun), "GetShieldAmt"),
			postfix: new HarmonyMethod(GetType(), nameof(ShieldGun_GetShieldAmt_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Converter), "GetShieldAmt"),
			postfix: new HarmonyMethod(GetType(), nameof(Converter_GetShieldAmt_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Converter), nameof(Converter.GetActions)),
			postfix: new HarmonyMethod(GetType(), nameof(Converter_GetActions_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Inverter), "GetShieldAmt"),
			postfix: new HarmonyMethod(GetType(), nameof(Inverter_GetShieldAmt_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Inverter), nameof(Converter.GetActions)),
			postfix: new HarmonyMethod(GetType(), nameof(Inverter_GetActions_Postfix))
		);
	}

	protected internal override void RegisterArt(ISpriteRegistry registry, string namePrefix)
	{
		base.RegisterArt(registry, namePrefix);
		ShieldCostSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Icon.ShieldCost",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Icons", "ShieldCost.png"))
		);
	}

	private static bool AnyStatusState
		=> OldStatusStates.Count != 0;

	private static void PushStatusState(Ship ship)
		=> OldStatusStates.Push((Shield: ship.Get(Status.shield), MaxShield: ship.Get(Status.maxShield), Shards: ship.Get(Status.shard), MaxShards: ship.Get(Status.maxShard)));

	internal static int GetRealStatus(Ship ship, Status status)
	{
		if (!AnyStatusState)
			return ship.Get(status);
		return status switch
		{
			Status.shield => OldStatusStates.Last().Shield,
			Status.maxShield => OldStatusStates.Last().MaxShield,
			Status.shard => OldStatusStates.Last().Shards,
			Status.maxShard => OldStatusStates.Last().MaxShards,
			_ => ship.Get(status)
		};
	}

	private static (int Shield, int Shards) PopStatusState()
	{
		var (shield, shards, _, _) = OldStatusStates.Pop();
		return (shield, shards);
	}

	private static bool TryPeekStatusState([MaybeNullWhen(false)] out int shield, [MaybeNullWhen(false)] out int shards)
	{
		if (OldStatusStates.TryPeek(out var oldState))
		{
			shield = oldState.Shield;
			shards = oldState.Shards;
			return true;
		}
		else
		{
			shield = default;
			shards = default;
			return false;
		}
	}

	private static void TemporarilyApplyShieldToShards(State state)
	{
		if (TemporarilyAppliedShieldToShardsCounter > 0)
			return;
		if (!state.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;

		int shield = state.ship.Get(Status.shield);
		PushStatusState(state.ship);
		state.ship.Add(Status.maxShard, shield);
		state.ship.Add(Status.shard, shield);
		TemporarilyAppliedShieldToShardsCounter++;
	}

	private static void UnapplyTemporarilyAppliedShieldFromShards(State state)
	{
		if (TemporarilyAppliedShieldToShardsCounter <= 0)
			return;
		if (!state.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;

		TemporarilyAppliedShieldToShardsCounter--;
		if (TemporarilyAppliedShieldToShardsCounter > 0)
			return;

		var (oldShield, _) = PopStatusState();
		state.ship.Add(Status.shard, -oldShield);
		state.ship.Add(Status.maxShard, -oldShield);
	}

	private static void Ship_NormalDamage_Prefix(Ship __instance, State s)
	{
		if (__instance != s.ship)
			return;
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;

		int shards = __instance.Get(Status.shield);
		PushStatusState(__instance);
		__instance.Add(Status.maxShield, shards);
		__instance.Add(Status.shield, shards);
	}

	private static void Ship_NormalDamage_Finalizer(Ship __instance, State s, Combat c)
	{
		if (__instance != s.ship)
			return;
		var artifact = s.EnumerateAllArtifacts().FirstOrDefault(a => a is BooksDizzyArtifact);
		if (artifact is null)
			return;

		var (_, oldShards) = PopStatusState();
		int currentShield = __instance.Get(Status.shield);
		int currentShards = __instance.Get(Status.shard);

		int resultingShield = currentShield - oldShards;

		int shardsToRemove = Math.Max(0, -resultingShield);
		resultingShield += shardsToRemove;
		int resultingShards = currentShards - shardsToRemove;

		int extraHullToRemove = Math.Max(0, -resultingShards);
		resultingShards += extraHullToRemove;

		if (resultingShards != currentShards)
		{
			__instance.Add(Status.shard, resultingShards - currentShards);
			artifact.Pulse();
		}
		if (resultingShield != currentShield)
			__instance.Add(Status.shield, resultingShield - currentShield);

		__instance.Add(Status.maxShield, -oldShards);
		if (extraHullToRemove > 0)
			__instance.DirectHullDamage(s, c, extraHullToRemove);
	}

	private static void Combat_DrainCardActions_Prefix(G g)
		=> TemporarilyApplyShieldToShards(g.state);

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
		int currentShards = g.state.ship.Get(Status.shard);
		int currentShield = g.state.ship.Get(Status.shield);

		int resultingShards = currentShards - oldShield;

		int shieldToRemove = Math.Max(0, -resultingShards);
		resultingShards += shieldToRemove;
		int resultingShield = currentShield - shieldToRemove;

		int leftOverToRemove = Math.Max(0, -resultingShield);
		resultingShield += leftOverToRemove;

		if (resultingShield != currentShield)
		{
			g.state.ship.Add(Status.shield, resultingShield - currentShield);
			artifact.Pulse();
		}
		if (resultingShards != currentShards)
			g.state.ship.Add(Status.shard, resultingShards - currentShards);

		g.state.ship.Add(Status.maxShard, -oldShield);
		// honestly not sure what to do about this?
		//if (leftOverToRemove > 0)
		//	g.state.ship.DirectHullDamage(s, c, leftOverToRemove);
	}

	private static void Card_MakeAllActionIcons_Prefix(State s)
		=> TemporarilyApplyShieldToShards(s);

	private static void Card_MakeAllActionIcons_Finalizer(State s)
		=> UnapplyTemporarilyAppliedShieldFromShards(s);

	private static void CardAction_GetTooltipsForActions_Prefix(State s)
		=> TemporarilyApplyShieldToShards(s);

	private static void CardAction_GetTooltipsForActions_Finalizer(State s)
		=> UnapplyTemporarilyAppliedShieldFromShards(s);

	private static void Card_RenderAction_Prefix(int shardAvailable)
		=> RenderActionShardAvailable = shardAvailable;

	private static IEnumerable<CodeInstruction> Card_RenderAction_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(4),
					ILMatches.Ldloc<int>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Instruction(OpCodes.Clt),
					ILMatches.LdcI4(0),
					ILMatches.Instruction(OpCodes.Ceq),
					ILMatches.AnyLdloca,
					ILMatches.Instruction(OpCodes.Call)
				)

				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.ExtractLabels(out var labels)

				.Advance()
				.CreateLdlocInstruction(out var ldlocLoopIterator)

				.Advance(-1)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.JustInsertion,
					ldlocLoopIterator.WithLabels(labels),
					new CodeInstruction(OpCodes.Stsfld, AccessTools.DeclaredField(typeof(BooksDizzyArtifact), nameof(ShardCostIconIndex)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static IEnumerable<CodeInstruction> Card_RenderAction_ShardcostIcon_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Brtrue,
					ILMatches.LdcI4((int)Enum.Parse<Spr>("icons_shardcostoff")),
					ILMatches.Br,
					ILMatches.LdcI4((int)Enum.Parse<Spr>("icons_shardcost"))
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
	}

	private static int Card_RenderAction_ShardcostIcon_Transpiler_ModifySprite(int spriteId, bool costMet)
	{
		if (!costMet)
			return spriteId;
		if (StateExt.Instance is not { } state || !state.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return spriteId;
		if (!TryPeekStatusState(out var shield, out var shards))
			return spriteId;

		int totalIndex = ShardCostIconIndex + (shards + shield - RenderActionShardAvailable);
		if (totalIndex > shards)
			return ShieldCostSprite.Id!.Value;
		return spriteId;
	}

	private static void ShieldGun_GetShieldAmt_Postfix(State s, ref int __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result += GetRealStatus(s.ship, Status.shard);
	}

	private static void Converter_GetShieldAmt_Postfix(State s, ref int __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result += GetRealStatus(s.ship, Status.shard);
	}

	private static void Converter_GetActions_Postfix(Converter __instance, State s, ref List<CardAction> __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		if (__instance.upgrade != Upgrade.B)
		{
			__result.Insert(0, new ADummyAction());
			__result.Add(new HiddenAStatus
			{
				status = Status.shard,
				statusAmount = 0,
				mode = AStatusMode.Set,
				targetPlayer = true,
				omitFromTooltips = true
			});
		}
	}

	private static void Inverter_GetShieldAmt_Postfix(State s, ref int __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result += GetRealStatus(s.ship, Status.shard);
	}

	private static void Inverter_GetActions_Postfix(Converter __instance, State s, ref List<CardAction> __result)
	{
		if (!s.EnumerateAllArtifacts().Any(a => a is BooksDizzyArtifact))
			return;
		__result.Insert(0, new ADummyAction());
		__result.Add(new HiddenAStatus
		{
			status = Status.shard,
			statusAmount = 0,
			mode = AStatusMode.Set,
			targetPlayer = true,
			omitFromTooltips = true
		});
	}
}
