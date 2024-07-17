using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

internal static class ShipPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly Dictionary<Status, (IReadOnlyList<Color> Colors, int? BarTickWidth)> StatusBarRenderingOverrides = [];

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnBeginTurn_Prefix_First)), Priority.First),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnBeginTurn_Postfix_Last)), Priority.Last),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnBeginTurn_Transpiler_Last)), Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnAfterTurn)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnAfterTurn_Prefix_First)), Priority.First),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnAfterTurn_Transpiler_Last)), Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), "GetStatusSize"),
			postfix: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_GetStatusSize_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), "RenderStatuses"),
			prefix: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_RenderStatuses_Prefix)),
			transpiler: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_RenderStatuses_Transpiler)),
			finalizer: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_RenderStatuses_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), "RenderStatusRow"),
			transpiler: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_RenderStatusRow_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Set)),
			prefix: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_Set_Prefix))
		);
	}

	private static void Ship_OnBeginTurn_Prefix_First(Ship __instance, State s, Combat c)
	{
		Instance.StatusLogicManager.OnTurnStart(s, c, __instance);
	}

	private static void Ship_OnBeginTurn_Postfix_Last(Ship __instance, State s, Combat c)
	{
		if (!__instance.isPlayerShip)
			return;
		Instance.MidrowScorchingManager.OnPlayerTurnStart(s, c);
	}

	private static IEnumerable<CodeInstruction> Ship_OnBeginTurn_Transpiler_Last(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.LdcI4((int)Status.timeStop),
					ILMatches.Call("Get")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Pop),
					new CodeInstruction(OpCodes.Ldc_I4_1)
				)
				.Find(ILMatches.Call("QueueImmediate"))
				.Replace(
					new CodeInstruction(OpCodes.Pop),
					new CodeInstruction(OpCodes.Pop)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Ship_OnAfterTurn_Prefix_First(Ship __instance, State s, Combat c)
	{
		Instance.StatusLogicManager.OnTurnEnd(s, c, __instance);
	}

	private static IEnumerable<CodeInstruction> Ship_OnAfterTurn_Transpiler_Last(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.LdcI4((int)Status.timeStop),
					ILMatches.Call("Get")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Pop),
					new CodeInstruction(OpCodes.Ldc_I4_1)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Ship_GetStatusSize_Postfix(Ship __instance, Status status, int amount, ref Ship.StatusPlan __result)
	{
		if (MG.inst.g.state is not { } state)
			return;
		if (state.route is not Combat combat)
			return;

		var hook = Instance.StatusRenderManager.GetOverridingAsBarsHook(state, combat, __instance, status, amount);
		if (hook is null)
			return;
		var @override = hook.OverrideStatusRendering(state, combat, __instance, status, amount);
		StatusBarRenderingOverrides[status] = @override;

		var barTickWidth = @override.BarTickWidth ?? __result.barTickWidth;
		if (@override.BarTickWidth != 0)
			__result.barTickWidth = barTickWidth;

		__result.asText = false;
		__result.asBars = true;
		__result.barMax = @override.Colors.Count;
		__result.boxWidth = 17 + @override.Colors.Count * (barTickWidth + 1);
	}

	private static void Ship_RenderStatuses_Prefix(Ship __instance)
		=> Instance.StatusRenderManager.RenderingStatusForShip = __instance;

	private static void Ship_RenderStatuses_Finalizer()
		=> Instance.StatusRenderManager.RenderingStatusForShip = null;

	private static IEnumerable<CodeInstruction> Ship_RenderStatuses_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.LdcI4(0).ExtractLabels(out var labels),
					ILMatches.Stloc<int>(originalMethod),
					ILMatches.LdcI4(0),
					ILMatches.Stloc<int>(originalMethod),
					ILMatches.Br,
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_RenderStatuses_Transpiler_ModifyStatusesToShow))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void Ship_RenderStatuses_Transpiler_ModifyStatusesToShow(Ship ship, G g)
	{
		var combat = g.state.route as Combat ?? DB.fakeCombat;

		ship._statusListCache = ship.statusEffects
			.Where(kvp => kvp.Key != Status.shield && kvp.Key != Status.tempShield)
			.Select(kvp => (Status: kvp.Key, Priority: 0.0, Amount: kvp.Value))
			.Concat(
				Instance.StatusRenderManager
					.SelectMany(hook => hook.GetExtraStatusesToShow(g.state, combat, ship))
					.Select(e => (Status: e.Status, Priority: e.Priority, Amount: ship.Get(e.Status)))
			)
			.OrderByDescending(e => e.Priority)
			.DistinctBy(e => e.Status)
			.Where(e => Instance.StatusRenderManager.ShouldShowStatus(g.state, combat, ship, e.Status, e.Amount))
			.Select(e => new KeyValuePair<Status, int>(e.Status, e.Amount))
			.ToList();
	}

	private static IEnumerable<CodeInstruction> Ship_RenderStatusRow_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldloca<KeyValuePair<Status, int>>(originalMethod).CreateLdlocInstruction(out var ldlocKvp),
					ILMatches.Call("get_Value"),
					ILMatches.Call("GetStatusSize"),
				])
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldloc<int>(originalMethod).CreateLdlocInstruction(out var ldlocBarIndex),
					ILMatches.Ldloc<Ship.StatusPlan>(originalMethod).CreateLdlocInstruction(out var ldlocStatusPlan),
					ILMatches.Ldfld("barMax"),
					ILMatches.Blt,
				])
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldloca<Color>(originalMethod),
					ILMatches.Instruction(OpCodes.Ldc_R8),
					ILMatches.Call("fadeAlpha"),
					ILMatches.Br.GetBranchTarget(out var branchTarget),
				])
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					ldlocBarIndex.Value.WithLabels(labels),
					ldlocKvp,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_RenderStatusRow_Transpiler_ModifyColor))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static Color Ship_RenderStatusRow_Transpiler_ModifyColor(Color color, int barIndex, KeyValuePair<Status, int> kvp)
	{
		if (!StatusBarRenderingOverrides.TryGetValue(kvp.Key, out var @override))
			return color;
		return @override.Colors[barIndex];
	}

	private static bool Ship_Set_Prefix(Ship __instance, Status status, ref int n)
	{
		if (MG.inst.g.state is not { } state)
			return true;
		if (state.route is not Combat combat)
			return true;

		int oldAmount = __instance.Get(status);
		int newAmount = Instance.StatusLogicManager.ModifyStatusChange(state, combat, __instance, status, oldAmount, n);

		if (newAmount == oldAmount)
			return false;
		n = newAmount;
		return true;
	}
}
