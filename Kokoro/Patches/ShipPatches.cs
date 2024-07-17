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

	private static (IReadOnlyList<Color> Colors, int? BarTickWidth)? LastStatusBarRenderingOverride;

	private static readonly Lazy<Func<Ship, G, Status, int, int>> GetStatusSizeBoxWidthMethod = new(() =>
	{
		var statusPlanType = AccessTools.Inner(typeof(Ship), "StatusPlan");
		var boxWidth = AccessTools.DeclaredField(statusPlanType, "boxWidth");
		var getStatusSizeMethod = AccessTools.DeclaredMethod(typeof(Ship), "GetStatusSize");

		DynamicMethod method = new("GetStatusSizeBoxWidth", typeof(int), [typeof(Ship), typeof(G), typeof(Status), typeof(int)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Ldarg_2);
		il.Emit(OpCodes.Ldarg_3);
		il.Emit(OpCodes.Call, getStatusSizeMethod);
		il.Emit(OpCodes.Ldfld, boxWidth);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Func<Ship, G, Status, int, int>>();
	});

	private static readonly Lazy<Action<Ship, G, string, List<KeyValuePair<Status, int>>, int, int, int>> RenderStatusRowMethod = new(() =>
	{
		var renderStatusRowMethod = AccessTools.DeclaredMethod(typeof(Ship), "RenderStatusRow");

		DynamicMethod method = new("RenderStatusRow", typeof(void), [typeof(Ship), typeof(G), typeof(string), typeof(List<KeyValuePair<Status, int>>), typeof(int), typeof(int), typeof(int)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Ldarg_2);
		il.Emit(OpCodes.Ldarg_3);
		il.Emit(OpCodes.Ldarg, 4);
		il.Emit(OpCodes.Ldarg, 5);
		il.Emit(OpCodes.Ldarg, 6);
		il.Emit(OpCodes.Call, renderStatusRowMethod);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<Ship, G, string, List<KeyValuePair<Status, int>>, int, int, int>>();
	});

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

	private static void Ship_GetStatusSize_Postfix(Ship __instance, Status status, int amount, ref object __result)
	{
		LastStatusBarRenderingOverride = null;
		if (MG.inst.g.state is not { } state)
			return;
		if (state.route is not Combat combat)
			return;

		var hook = Instance.StatusRenderManager.GetOverridingAsBarsHook(state, combat, __instance, status, amount);
		if (hook is null)
			return;
		var @override = hook.OverrideStatusRendering(state, combat, __instance, status, amount);
		LastStatusBarRenderingOverride = @override;

		// TODO: use a publicizer, or emit some IL to do this instead. performance must suck
		var statusPlanType = AccessTools.Inner(typeof(Ship), "StatusPlan");
		var asTextField = AccessTools.DeclaredField(statusPlanType, "asText");
		var asBarsField = AccessTools.DeclaredField(statusPlanType, "asBars");
		var barMaxField = AccessTools.DeclaredField(statusPlanType, "barMax");
		var boxWidthField = AccessTools.DeclaredField(statusPlanType, "boxWidth");
		var barTickWidthField = AccessTools.DeclaredField(statusPlanType, "barTickWidth");

		int barTickWidth = @override.BarTickWidth ?? (int)barTickWidthField.GetValue(__result)!;
		if (@override.BarTickWidth != 0)
			barTickWidthField.SetValue(__result, barTickWidth);

		asTextField.SetValue(__result, false);
		asBarsField.SetValue(__result, true);
		barMaxField.SetValue(__result, @override.Colors.Count);
		boxWidthField.SetValue(__result, 17 + @override.Colors.Count * (barTickWidth + 1));
	}

	private static bool Ship_RenderStatuses_Prefix(Ship __instance, G g, string keyPrefix)
	{
		Instance.StatusRenderManager.RenderingStatusForShip = __instance;

		var combat = g.state.route as Combat ?? DB.fakeCombat;
		var toRender = __instance.statusEffects
			.Where(kvp => kvp.Key != Status.shield && kvp.Key != Status.tempShield)
			.Select(kvp => (Status: kvp.Key, Priority: 0.0, Amount: kvp.Value))
			.Concat(
				Instance.StatusRenderManager
					.SelectMany(hook => hook.GetExtraStatusesToShow(g.state, combat, __instance))
					.Select(e => (Status: e.Status, Priority: e.Priority, Amount: __instance.Get(e.Status)))
			)
			.OrderByDescending(e => e.Priority)
			.DistinctBy(e => e.Status)
			.Where(e => Instance.StatusRenderManager.ShouldShowStatus(g.state, combat, __instance, e.Status, e.Amount))
			.Select(e => new KeyValuePair<Status, int>(e.Status, e.Amount));

		List<KeyValuePair<Status, int>> currentRow = new();
		int currentRowLength = 0;
		int rowIndex = 0;

		void FinishRow()
		{
			if (currentRow.Count == 0)
				return;

			RenderStatusRowMethod.Value(__instance, g, keyPrefix, currentRow, rowIndex, 0, currentRow.Count);

			currentRow.Clear();
			currentRowLength = 0;
			rowIndex++;
		}

		foreach (var kvp in toRender)
		{
			var boxWidth = GetStatusSizeBoxWidthMethod.Value(__instance, g, kvp.Key, kvp.Value);

			if (currentRowLength + boxWidth > 142)
				FinishRow();
			currentRow.Add(kvp);
			currentRowLength += boxWidth;
		}
		FinishRow();
		return false;
	}

	private static void Ship_RenderStatuses_Finalizer()
		=> Instance.StatusRenderManager.RenderingStatusForShip = null;

	private static Dictionary<Status, int> Ship_RenderStatuses_Transpiler_ModifyStatusesToShow(Dictionary<Status, int> statusEffects, Ship ship)
	{
		if (MG.inst.g.state is not { } state)
			return statusEffects;
		if (state.route is not Combat combat)
			return statusEffects;

		return statusEffects
			.Select(kvp => (Status: kvp.Key, Priority: 0.0, Amount: kvp.Value))
			.Concat(
				Instance.StatusRenderManager
					.SelectMany(hook => hook.GetExtraStatusesToShow(state, combat, ship))
					.Select(e => (Status: e.Status, Priority: e.Priority, Amount: ship.Get(e.Status)))
			)
			.OrderByDescending(e => e.Priority)
			.DistinctBy(e => e.Status)
			.Where(e => Instance.StatusRenderManager.ShouldShowStatus(state, combat, ship, e.Status, e.Amount))
			.ToDictionary(e => e.Status, e => e.Amount);
	}

	private static IEnumerable<CodeInstruction> Ship_RenderStatusRow_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence,
					ILMatches.Call("get_Item"),
					ILMatches.Stloc<KeyValuePair<Status, int>>(originalMethod)
				)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence,
					ILMatches.Ldloc<int>(originalMethod).CreateLdlocInstruction(out var ldlocBarIndex),
					ILMatches.AnyLdloc,
					ILMatches.Ldfld("barMax"),
					ILMatches.Blt
				)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.Before,
					ILMatches.Ldloca<Color>(originalMethod),
					ILMatches.Instruction(OpCodes.Ldc_R8),
					ILMatches.Call("fadeAlpha"),
					ILMatches.Br,
					ILMatches.Ldloc<Color>(originalMethod)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					ldlocBarIndex.Value.WithLabels(labels),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_RenderStatusRow_Transpiler_ModifyColor)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static Color Ship_RenderStatusRow_Transpiler_ModifyColor(Color color, int barIndex)
	{
		if (LastStatusBarRenderingOverride is null)
			return color;
		return LastStatusBarRenderingOverride.Value.Colors[barIndex];
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
