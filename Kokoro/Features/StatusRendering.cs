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
			
			internal record struct GetExtraStatusesToShowArgs(
				State State,
				Combat Combat,
				Ship Ship
			) : IKokoroApi.IV2.IStatusRenderingApi.IHook.IGetExtraStatusesToShowArgs;
			
			internal record struct ShouldShowStatusArgs(
				State State,
				Combat Combat,
				Ship Ship,
				Status Status,
				int Amount
			) : IKokoroApi.IV2.IStatusRenderingApi.IHook.IShouldShowStatusArgs;
			
			internal record struct ShouldOverrideStatusRenderingAsBarsArgs(
				State State,
				Combat Combat,
				Ship Ship,
				Status Status,
				int Amount
			) : IKokoroApi.IV2.IStatusRenderingApi.IHook.IShouldOverrideStatusRenderingAsBarsArgs;
			
			internal record struct OverrideStatusRenderingArgs(
				State State,
				Combat Combat,
				Ship Ship,
				Status Status,
				int Amount
			) : IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingArgs;
			
			internal record struct OverrideStatusTooltipsArgs(
				Status Status,
				int Amount,
				Ship? Ship,
				List<Tooltip> Tooltips
			) : IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs;
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
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			if (hook.ShouldShowStatus(new ApiImplementation.V2Api.StatusRenderingApi.ShouldShowStatusArgs(state, combat, ship, status, amount)) is { } result)
				return result;
		return true;
	}

	public IKokoroApi.IV2.IStatusRenderingApi.IHook? GetOverridingAsBarsHook(State state, Combat combat, Ship ship, Status status, int amount)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
		{
			var hookResult = hook.ShouldOverrideStatusRenderingAsBars(new ApiImplementation.V2Api.StatusRenderingApi.ShouldOverrideStatusRenderingAsBarsArgs(state, combat, ship, status, amount));
			switch (hookResult)
			{
				case false:
					return null;
				case true:
					return hook;
			}
		}
		return null;
	}

	internal List<Tooltip> OverrideStatusTooltips(Status status, int amount, List<Tooltip> tooltips)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, (MG.inst.g.state ?? DB.fakeState).EnumerateAllArtifacts()))
			tooltips = hook.OverrideStatusTooltips(new ApiImplementation.V2Api.StatusRenderingApi.OverrideStatusTooltipsArgs(status, amount, RenderingStatusForShip, tooltips));
		return tooltips;
	}
	
	private static void StatusMeta_GetTooltips_Postfix(Status status, int amt, ref List<Tooltip> __result)
		=> __result = Instance.OverrideStatusTooltips(status, amt, __result);
	
	private static void Ship_GetStatusSize_Postfix(Ship __instance, Status status, int amount, ref Ship.StatusPlan __result)
	{
		if (MG.inst.g.state is not { } state)
			return;
		if (state.route is not Combat combat)
			return;

		var hook = Instance.GetOverridingAsBarsHook(state, combat, __instance, status, amount);
		if (hook is null)
			return;
		var @override = hook.OverrideStatusRendering(new ApiImplementation.V2Api.StatusRenderingApi.OverrideStatusRenderingArgs(state, combat, __instance, status, amount));
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

		ship._statusListCache = ship.statusEffects
			.Where(kvp => kvp.Key != Status.shield && kvp.Key != Status.tempShield)
			.Select(kvp => (Status: kvp.Key, Priority: 0.0, Amount: kvp.Value))
			.Concat(
				Instance
					.SelectMany(hook => hook.GetExtraStatusesToShow(new ApiImplementation.V2Api.StatusRenderingApi.GetExtraStatusesToShowArgs(g.state, combat, ship)))
					.Select(e => (Status: e.Status, Priority: e.Priority, Amount: ship.Get(e.Status)))
			)
			.OrderByDescending(e => e.Priority)
			.DistinctBy(e => e.Status)
			.Where(e => Instance.ShouldShowStatus(g.state, combat, ship, e.Status, e.Amount))
			.Select(e => new KeyValuePair<Status, int>(e.Status, e.Amount))
			.ToList();
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
		
	public bool? ShouldOverrideStatusRenderingAsBars(IKokoroApi.IV2.IStatusRenderingApi.IHook.IShouldOverrideStatusRenderingAsBarsArgs args)
		=> v1.ShouldOverrideStatusRenderingAsBars(args.State, args.Combat, args.Ship, args.Status, args.Amount);
		
	public (IReadOnlyList<Color> Colors, int? BarTickWidth) OverrideStatusRendering(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingArgs args)
		=> v1.OverrideStatusRendering(args.State, args.Combat, args.Ship, args.Status, args.Amount);
		
	public List<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
		=> v1.OverrideStatusTooltips(args.Status, args.Amount, args.Ship, args.Tooltips);
}