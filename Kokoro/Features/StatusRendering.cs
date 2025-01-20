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

			public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer EmptyStatusInfoRenderer
				=> Kokoro.EmptyStatusInfoRenderer.Instance;
			
			public IKokoroApi.IV2.IStatusRenderingApi.ITextStatusInfoRenderer MakeTextStatusInfoRenderer(string text)
				=> new TextStatusInfoRenderer { Text = text };

			public IKokoroApi.IV2.IStatusRenderingApi.ITextStatusInfoRenderer? AsTextStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer renderer)
				=> renderer as IKokoroApi.IV2.IStatusRenderingApi.ITextStatusInfoRenderer;

			public IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer MakeBarStatusInfoRenderer()
				=> new BarStatusInfoRenderer();

			public IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer? AsBarStatusInfoRenderer(IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer renderer)
				=> renderer as IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer;

			internal sealed class StatusInfoRendererRenderArgs : IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer.IRenderArgs
			{
				public G G { get; internal set; } = null!;
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
				public Status Status { get; internal set; }
				public int Amount { get; internal set; }
				public bool DontRender { get; internal set; }
				public Vec Position { get; internal set; }
			}
			
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
			
			internal sealed class OverrideStatusInfoRendererArgs : IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusInfoRendererArgs
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
	private static readonly Dictionary<Status, IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer> StatusInfoRenderers = [];

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

	public IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer? GetStatusInfoRenderer(State state, Combat combat, Ship ship, Status status, int amount)
	{
		var overrideStatusInfoRendererArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusRenderingApi.OverrideStatusInfoRendererArgs>();
		var overrideStatusRenderingAsBarsArgs = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusRenderingApi.OverrideStatusRenderingAsBarsArgs>();
		
		try
		{
			overrideStatusInfoRendererArgs.State = state;
			overrideStatusInfoRendererArgs.Combat = combat;
			overrideStatusInfoRendererArgs.Ship = ship;
			overrideStatusInfoRendererArgs.Status = status;
			overrideStatusInfoRendererArgs.Amount = amount;
			
			overrideStatusRenderingAsBarsArgs.State = state;
			overrideStatusRenderingAsBarsArgs.Combat = combat;
			overrideStatusRenderingAsBarsArgs.Ship = ship;
			overrideStatusRenderingAsBarsArgs.Status = status;
			overrideStatusRenderingAsBarsArgs.Amount = amount;

			foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()))
			{
				if (hook.OverrideStatusInfoRenderer(overrideStatusInfoRendererArgs) is { } renderer)
					return renderer;

#pragma warning disable CS0618 // Type or member is obsolete
				if (hook.OverrideStatusRenderingAsBars(overrideStatusRenderingAsBarsArgs) is { } @override)
				{
					if (@override.Colors.Count == 0)
						return ModEntry.Instance.Api.V2.StatusRendering.EmptyStatusInfoRenderer;
					return ModEntry.Instance.Api.V2.StatusRendering.MakeBarStatusInfoRenderer().SetSegments(@override.Colors);
				}
#pragma warning restore CS0618 // Type or member is obsolete
			}

			return null;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(overrideStatusInfoRendererArgs);
			ModEntry.Instance.ArgsPool.Return(overrideStatusRenderingAsBarsArgs);
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
		if (MG.inst.g is not { } g)
			return;
		if (g.state is not { } state)
			return;
		if (state.route is not Combat combat)
			return;
		if (Instance.GetStatusInfoRenderer(state, combat, __instance, status, amount) is not { } renderer)
			return;
		
		StatusInfoRenderers[status] = renderer;

		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusRenderingApi.StatusInfoRendererRenderArgs>();
		try
		{
			args.G = g;
			args.State = state;
			args.Combat = combat;
			args.Ship = __instance;
			args.Status = status;
			args.Amount = amount;
			args.DontRender = true;
			args.Position = default;

			__result.boxWidth = 16 + renderer.Render(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
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

	private static IEnumerable<CodeInstruction> Ship_RenderStatusRow_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
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
				.PointerMatcher(SequenceMatcherRelativeElement.FirstInWholeSequence)
				.Find([
					ILMatches.Ldloc<Ship.StatusPlan>(originalMethod).CreateLdlocaInstruction(out var ldlocaStatusPlan).ExtractLabels(out var labels).Anchor(out var renderingStartAnchor),
					ILMatches.Ldfld("asText"),
					ILMatches.Brfalse,
				])
				.Find([
					ILMatches.Ldarg(0).CreateLabel(il, out var renderingEndLabel),
					ILMatches.Ldfld("pendingOneShotStatusAnimations"),
				])
				.Anchors().PointerMatcher(renderingStartAnchor)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					ldlocKvp,
					ldlocaStatusPlan,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderStatusRow_Transpiler_HandleCustomRenderers))).WithLabels(labels),
					new CodeInstruction(OpCodes.Brtrue, renderingEndLabel),
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

	private static bool Ship_RenderStatusRow_Transpiler_HandleCustomRenderers(Ship ship, G g, string keyPrefix, KeyValuePair<Status, int> kvp, ref Ship.StatusPlan statusPlan)
	{
		if (g.state is not { } state)
			return false;
		if (state.route is not Combat combat)
			return false;
		if (!StatusInfoRenderers.Remove(kvp.Key, out var renderer))
			return false;
		if (g.boxes.FirstOrDefault(b => b.key == new UIKey(StableUK.status, (int)kvp.Key, keyPrefix)) is not { } box)
			return false;

		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.StatusRenderingApi.StatusInfoRendererRenderArgs>();
		try
		{
			args.G = g;
			args.State = state;
			args.Combat = combat;
			args.Ship = ship;
			args.Status = kvp.Key;
			args.Amount = kvp.Value;
			args.DontRender = false;
			args.Position = new Vec(box.rect.x + 12, box.rect.y + 4);

			renderer.Render(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
		return true;
	}
}

internal sealed class EmptyStatusInfoRenderer : IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer
{
	public static readonly EmptyStatusInfoRenderer Instance = new();
	
	public int Render(IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer.IRenderArgs args)
		=> -1;
}

internal sealed class TextStatusInfoRenderer : IKokoroApi.IV2.IStatusRenderingApi.ITextStatusInfoRenderer
{
	public required string Text { get; set; }
	public Color Color { get; set; } = Colors.white;

	public int Render(IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer.IRenderArgs args)
		=> (int)Draw.Text(Text, args.Position.x + 1, args.Position.y, color: this.Color, dontDraw: args.DontRender, outline: Colors.black, dontSubstituteLocFont: true).w;

	public IKokoroApi.IV2.IStatusRenderingApi.ITextStatusInfoRenderer SetText(string value)
	{
		this.Text = value;
		return this;
	}

	public IKokoroApi.IV2.IStatusRenderingApi.ITextStatusInfoRenderer SetColor(Color value)
	{
		this.Color = value;
		return this;
	}
}

internal sealed class BarStatusInfoRenderer : IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer
{
	public IList<Color> Segments { get; set; } = new List<Color>();

	public int Rows
	{
		get => this.RowsStorage;
		set
		{
			if (value is not (1 or 2 or 3 or 5))
				throw new ArgumentException("Only 1, 2, 3 or 5 rows are supported");
			this.RowsStorage = value;
		}
	}

	public int SegmentWidth { get; set; } = 2;
	public int HorizontalSpacing { get; set; } = 1;

	private int RowsStorage = 1;

	public int Render(IKokoroApi.IV2.IStatusRenderingApi.IStatusInfoRenderer.IRenderArgs args)
	{
		if (this.Segments.Count == 0)
			return -1;
		
		const int xOffset = 2;
		
		var columns = (this.Segments.Count + this.Rows - 1) / this.Rows;

		for (var i = 0; i < this.Segments.Count; i++)
		{
			var column = i / this.Rows;
			var row = i % this.Rows;
			var (yOffset, height) = GetLayoutParameters();
			
			if (!args.DontRender)
				Draw.Rect(args.Position.x + xOffset + (this.SegmentWidth + this.HorizontalSpacing) * column, args.Position.y + yOffset, this.SegmentWidth, height, this.Segments[i]);

			(int YOffset, int Height) GetLayoutParameters()
			{
				// 1-row columns are always full
				if (this.Rows == 1)
					return (0, 5);

				var normalHeight = this.Rows switch
				{
					2 => 2,
					3 or 5 => 1,
					_ => throw new ArgumentOutOfRangeException()
				};
				var verticalSpacing = this.Rows switch
				{
					2 or 3 => 1,
					5 => 0,
					_ => throw new ArgumentOutOfRangeException()
				};
				
				// ignore full columns
				var segmentsInColumn = Math.Min(this.Segments.Count - column * this.Rows, this.Rows);
				if (segmentsInColumn == this.Rows)
					return ((normalHeight + verticalSpacing) * row, normalHeight);

				// there's only one way a 2-row column is not full
				if (this.Rows == 2)
					return (1, normalHeight + 1);

				if (this.Rows == 3)
				{
					if (segmentsInColumn == 1)
						return (2, normalHeight);
					if (segmentsInColumn == 2)
						return (1 + (normalHeight + verticalSpacing) * row, normalHeight);
					throw new ArgumentOutOfRangeException();
				}

				if (this.Rows == 5)
				{
					var unfullColumnYOffset = (this.Rows - segmentsInColumn) / 2;
					return (unfullColumnYOffset + row, normalHeight);
				}
				
				throw new ArgumentOutOfRangeException();
			}
		}
		
		return xOffset + columns * this.SegmentWidth + (columns - 1) * this.HorizontalSpacing;
	}
	
	public IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer SetSegments(IEnumerable<Color> value)
	{
		this.Segments = value as IList<Color> ?? value.ToList();
		return this;
	}

	public IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer SetSegmentWidth(int value)
	{
		this.SegmentWidth = value;
		return this;
	}

	public IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer SetHorizontalSpacing(int value)
	{
		this.HorizontalSpacing = value;
		return this;
	}

	public IKokoroApi.IV2.IStatusRenderingApi.IBarStatusInfoRenderer SetRows(int value)
	{
		this.Rows = value;
		return this;
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