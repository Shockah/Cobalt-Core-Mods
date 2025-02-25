﻿using HarmonyLib;
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

partial class ApiImplementation
{
	#region V1
	
	public void RegisterStatusRenderHook(IStatusRenderHook hook, double priority)
		=> StatusRenderManager.Instance.Register(hook, priority);

	public void UnregisterStatusRenderHook(IStatusRenderHook hook)
		=> StatusRenderManager.Instance.Unregister(hook);

	public Color DefaultActiveStatusBarColor
		=> new("b2f2ff");

	public Color DefaultInactiveStatusBarColor
		=> DefaultActiveStatusBarColor.fadeAlpha(0.3);

	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IStatusRenderingApi StatusRendering { get; } = new StatusRenderingApi();
		
		public sealed class StatusRenderingApi : IKokoroApi.IV2.IStatusRenderingApi
		{
			public void RegisterHook(IKokoroApi.IV2.IStatusRenderingApi.IHook hook, double priority = 0)
				=> StatusRenderManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IStatusRenderingApi.IHook hook)
				=> StatusRenderManager.Instance.Unregister(hook);
			
			public Color DefaultActiveStatusBarColor
				=> new("b2f2ff");

			public Color DefaultInactiveStatusBarColor
				=> DefaultActiveStatusBarColor.fadeAlpha(0.3);
			
			internal sealed class GetExtraStatusesToShowArgs : IKokoroApi.IV2.IStatusRenderingApi.IHook.IGetExtraStatusesToShowArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
			}
			
			internal sealed class ShouldShowStatusArgs : IKokoroApi.IV2.IStatusRenderingApi.IHook.IShouldShowStatusArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
				public int Amount { get; internal set; }
			}
			
			internal sealed class OverrideStatusRenderingAsBarsArgs : IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingAsBarsArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
				public int Amount { get; internal set; }
			}
			
			internal sealed class OverrideStatusTooltipsArgs : IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs
			{
				public Status Status { get; internal set; }
				public int Amount { get; internal set; }
				public Ship? Ship { get; internal set; }
				public IReadOnlyList<Tooltip> Tooltips { get; internal set; } = null!;
			}
		}
	}
}

internal sealed class StatusRenderManager : VariedApiVersionHookManager<IKokoroApi.IV2.IStatusRenderingApi.IHook, IStatusRenderHook>
{
	internal static readonly StatusRenderManager Instance = new();

	private Ship? RenderingStatusForShip;
	private static readonly Dictionary<Status, (IReadOnlyList<Color> Colors, int? BarTickWidth)> StatusBarRenderingOverrides = [];

	private StatusRenderManager() : base(ModEntry.Instance.Package.Manifest.UniqueName, new HookMapper<IKokoroApi.IV2.IStatusRenderingApi.IHook, IStatusRenderHook>(hook => new V1ToV2StatusRenderingHookWrapper(hook)))
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StatusMeta), nameof(StatusMeta.GetTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StatusMeta_GetTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.GetStatusSize)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_GetStatusSize_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.RenderStatuses)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderStatuses_Prefix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderStatuses_Transpiler)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderStatuses_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.RenderStatusRow)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderStatusRow_Transpiler))
		);
	}

	public bool ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusRenderingApi.ShouldShowStatusArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Ship = ship;
			args.Status = status;
			args.Amount = amount;

			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				if (hook.ShouldShowStatus(args) is { } result)
					return result;
			return true;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public (IReadOnlyList<Color> Colors, int? BarTickWidth)? GetStatusRenderingBarOverride(State state, Combat combat, Ship ship, Status status, int amount)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusRenderingApi.OverrideStatusRenderingAsBarsArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Ship = ship;
			args.Status = status;
			args.Amount = amount;

			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
				if (hook.OverrideStatusRenderingAsBars(args) is { } @override)
					return @override;
			return null;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	internal IReadOnlyList<Tooltip> OverrideStatusTooltips(Status status, int amount, IReadOnlyList<Tooltip> tooltips)
	{
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusRenderingApi.OverrideStatusTooltipsArgs>();
		try
		{
			args.Status = status;
			args.Amount = amount;
			args.Ship = RenderingStatusForShip;
			args.Tooltips = tooltips;

			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, (MG.inst.g.state ?? DB.fakeState).EnumerateAllArtifacts()))
				args.Tooltips = hook.OverrideStatusTooltips(args);
			return args.Tooltips;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}
	
	private static void StatusMeta_GetTooltips_Postfix(Status status, int amt, ref List<Tooltip> __result)
		=> __result = Instance.OverrideStatusTooltips(status, amt, __result).ToList();
	
	private static void Ship_GetStatusSize_Postfix(Ship __instance, Status status, int amount, ref Ship.StatusPlan __result)
	{
		if (MG.inst.g.state is not { } state)
			return;
		if (state.route is not Combat combat)
			return;
		if (Instance.GetStatusRenderingBarOverride(state, combat, __instance, status, amount) is not { } @override)
			return;
		
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
		=> Instance.RenderingStatusForShip = __instance;

	private static void Ship_RenderStatuses_Finalizer()
		=> Instance.RenderingStatusForShip = null;

	private static IEnumerable<CodeInstruction> Ship_RenderStatuses_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderStatuses_Transpiler_ModifyStatusesToShow))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void Ship_RenderStatuses_Transpiler_ModifyStatusesToShow(Ship ship, G g)
	{
		var combat = g.state.route as Combat ?? DB.fakeCombat;
		
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusRenderingApi.GetExtraStatusesToShowArgs>();
		try
		{
			args.State = g.state;
			args.Combat = combat;
			args.Ship = ship;

			ship._statusListCache = ship.statusEffects
				.Where(kvp => kvp.Key != Status.shield && kvp.Key != Status.tempShield)
				.Select(kvp => (Status: kvp.Key, Priority: 0.0, Amount: kvp.Value))
				.Concat(
					Instance
						.SelectMany(hook => hook.GetExtraStatusesToShow(args))
						.Select(e => (Status: e.Status, Priority: e.Priority, Amount: ship.Get(e.Status)))
				)
				.OrderByDescending(e => e.Priority)
				.DistinctBy(e => e.Status)
				.Where(e => Instance.ShouldShowStatus(g.state, combat, ship, e.Status, e.Amount))
				.Select(e => new KeyValuePair<Status, int>(e.Status, e.Amount))
				.ToList();
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	private static IEnumerable<CodeInstruction> Ship_RenderStatusRow_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
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
					ILMatches.Ldloc<Ship.StatusPlan>(originalMethod),
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderStatusRow_Transpiler_ModifyColor))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static Color Ship_RenderStatusRow_Transpiler_ModifyColor(Color color, int barIndex, KeyValuePair<Status, int> kvp)
	{
		if (!StatusBarRenderingOverrides.TryGetValue(kvp.Key, out var @override))
			return color;
		return @override.Colors[barIndex];
	}
}

internal sealed class V1ToV2StatusRenderingHookWrapper(IStatusRenderHook v1) : IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	public IEnumerable<(Status Status, double Priority)> GetExtraStatusesToShow(IKokoroApi.IV2.IStatusRenderingApi.IHook.IGetExtraStatusesToShowArgs args)
		=> v1.GetExtraStatusesToShow(args.State, args.Combat, args.Ship);
		
	public bool? ShouldShowStatus(IKokoroApi.IV2.IStatusRenderingApi.IHook.IShouldShowStatusArgs args)
		=> v1.ShouldShowStatus(args.State, args.Combat, args.Ship, args.Status, args.Amount);
	
	public (IReadOnlyList<Color> Colors, int? BarSegmentWidth)? OverrideStatusRenderingAsBars(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingAsBarsArgs args)
		=> v1.ShouldOverrideStatusRenderingAsBars(args.State, args.Combat, args.Ship, args.Status, args.Amount) == true ? v1.OverrideStatusRendering(args.State, args.Combat, args.Ship, args.Status, args.Amount) : null;
		
	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		=> v1.OverrideStatusTooltips(args.Status, args.Amount, args.Ship, args.Tooltips.ToList());
}