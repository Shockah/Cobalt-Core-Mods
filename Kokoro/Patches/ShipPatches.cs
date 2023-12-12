using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Shared;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System;
using System.Linq;

namespace Shockah.Kokoro;

internal static class ShipPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private static (IReadOnlyList<Color> Colors, int? BarTickWidth)? LastStatusBarRenderingOverride;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnBeginTurn_Postfix_Last)), Priority.Last)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnAfterTurn)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ShipPatches), nameof(Ship_OnAfterTurn_Prefix_First)), Priority.First)
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), "GetStatusSize"),
			postfix: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_GetStatusSize_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), "RenderStatuses"),
			prefix: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_RenderStatuses_Prefix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), "RenderStatusRow"),
			prefix: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_RenderStatusRow_Postfix)),
			transpiler: new HarmonyMethod(typeof(ShipPatches), nameof(Ship_RenderStatusRow_Transpiler))
		);
	}

	private static void Ship_OnBeginTurn_Postfix_Last(Ship __instance, State s, Combat c)
	{
		if (__instance != s.ship)
			return;

		Instance.MidrowScorchingManager.OnPlayerTurnStart(s, c);
		Instance.WormStatusManager.OnPlayerTurnStart(s, c);
	}

	private static void Ship_OnAfterTurn_Prefix_First(Ship __instance, State s)
	{
		Instance.OxidationStatusManager.OnTurnEnd(s, __instance);
	}

	private static void Ship_GetStatusSize_Postfix(Ship __instance, Status status, int amount, ref object __result)
	{
		LastStatusBarRenderingOverride = null;
		if (StateExt.Instance is not { } state)
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
		// TODO: use a publicizer, or emit some IL to do this instead. performance must suck
		var statusPlanType = AccessTools.Inner(typeof(Ship), "StatusPlan");
		var boxWidthField = AccessTools.DeclaredField(statusPlanType, "boxWidth");
		var getStatusSizeMethod = AccessTools.DeclaredMethod(typeof(Ship), "GetStatusSize");
		var renderStatusRowMethod = AccessTools.DeclaredMethod(typeof(Ship), "RenderStatusRow");

		var combat = g.state.route as Combat ?? DB.fakeCombat;

		var toRender = __instance.statusEffects
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

			renderStatusRowMethod.Invoke(__instance, new object?[] { g, keyPrefix, currentRow, rowIndex, 0, currentRow.Count });

			currentRow.Clear();
			currentRowLength = 0;
			rowIndex++;
		}

		foreach (var kvp in toRender)
		{
			var statusSize = getStatusSizeMethod.Invoke(__instance, new object?[] { g, kvp.Key, kvp.Value });
			int boxWidth = (int)boxWidthField.GetValue(statusSize)!;

			if (currentRowLength + boxWidth > 142)
				FinishRow();
			currentRow.Add(kvp);
			currentRowLength += boxWidth;
		}
		FinishRow();
		return false;
	}

	private static Dictionary<Status, int> Ship_RenderStatuses_Transpiler_ModifyStatusesToShow(Dictionary<Status, int> statusEffects, Ship ship)
	{
		if (StateExt.Instance is not { } state)
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

	private static void Ship_RenderStatusRow_Postfix(Ship __instance, G g)
	{
		Instance.OxidationStatusManager.ModifyStatusTooltips(__instance, g);
	}

	private static IEnumerable<CodeInstruction> Ship_RenderStatusRow_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence,
					ILMatches.Call("get_Item"),
					ILMatches.Stloc<KeyValuePair<Status, int>>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence,
					ILMatches.Ldloc<int>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.AnyLdloc,
					ILMatches.Ldfld("barMax"),
					ILMatches.Blt
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLdlocInstruction(out var ldlocBarIndex)
				.Find(
					SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.Before,
					ILMatches.Ldloca<Color>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Instruction(OpCodes.Ldc_R8),
					ILMatches.Call("fadeAlpha"),
					ILMatches.Br,
					ILMatches.Ldloc<Color>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					ldlocBarIndex.WithLabels(labels),
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
}
