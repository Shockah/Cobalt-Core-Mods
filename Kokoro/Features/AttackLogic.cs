using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.IAttackLogicApi AttackLogic { get; } = new AttackLogicApi();

		public sealed class AttackLogicApi : IKokoroApi.IV2.IAttackLogicApi
		{
			public void RegisterHook(IKokoroApi.IV2.IAttackLogicApi.IHook hook, double priority = 0)
				=> AttackLogicManager.Instance.Register(hook, priority);

			public void UnregisterHook(IKokoroApi.IV2.IAttackLogicApi.IHook hook)
				=> AttackLogicManager.Instance.Unregister(hook);
			
			internal sealed class ModifyHighlightRenderingArgs : IKokoroApi.IV2.IAttackLogicApi.IHook.IModifyHighlightRenderingArgs
			{
				public State State { get; internal set; } = null!;
				public Combat Combat { get; internal set; } = null!;
				public Ship Ship { get; internal set; } = null!;
				public Part Part { get; internal set; } = null!;
				public int WorldX { get; internal set; }
				public StuffBase? Object { get; internal set; }
				public Color HighlightColor { get; set; }
				public bool StopsInMidrow { get; set; }
			}
		}
	}
}

internal sealed class AttackLogicManager : HookManager<IKokoroApi.IV2.IAttackLogicApi.IHook>
{
	internal static readonly AttackLogicManager Instance = new();
	
	private AttackLogicManager() : base(ModEntry.Instance.Package.Manifest.UniqueName)
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.DrawIntentLinesForPart)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrawIntentLinesForPart_Transpiler))
		);
	}
	
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Combat_DrawIntentLinesForPart_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		try
		{
			var trueRenderDroneEndCapLabel = il.DefineLabel();
			var falseRenderDroneEndCapLabel = il.DefineLabel();
			var hookLabel = il.DefineLabel();
			var renderDroneEndCapLocal = il.DeclareLocal(typeof(bool));
			
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldsfld(nameof(Colors.attackStatusHintPlayer)),
					ILMatches.Stloc<Color>(originalMethod).GetLocalIndex(out var highlightColorLocalIndex),
				])
				.Find([
					ILMatches.Ldloc<StuffBase>(originalMethod),
					ILMatches.Isinst<Missile>(),
					ILMatches.Brfalse.GetBranchTarget(out var renderDroneEndCapLabel),
				])
				.PointerMatcher(renderDroneEndCapLabel)
				.Advance(-1)
				.GetBranchTarget(out var pastRenderDroneEndCapLabel)
				
				.BlockMatcher(SequenceMatcherRelativeBounds.WholeSequence)
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.ExcludingInsertion, [
					new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(trueRenderDroneEndCapLabel),
					new CodeInstruction(OpCodes.Stloc, renderDroneEndCapLocal),
					new CodeInstruction(OpCodes.Br, hookLabel),
					new CodeInstruction(OpCodes.Ldc_I4_0).WithLabels(falseRenderDroneEndCapLabel),
					new CodeInstruction(OpCodes.Stloc, renderDroneEndCapLocal),
					// new CodeInstruction(OpCodes.Br, hookLabel),
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(hookLabel),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_3),
					new CodeInstruction(OpCodes.Ldarg, 4),
					new CodeInstruction(OpCodes.Ldloc, renderDroneEndCapLocal),
					new CodeInstruction(OpCodes.Ldloca, highlightColorLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_DrawIntentLinesForPart_Transpiler_ModifyRendering))),
					new CodeInstruction(OpCodes.Brtrue, renderDroneEndCapLabel.Value),
					new CodeInstruction(OpCodes.Br, pastRenderDroneEndCapLabel),
				])
				.BlockMatcher(SequenceMatcherRelativeBounds.Before)
				.ForEach(
					SequenceMatcherRelativeBounds.Enclosed,
					[new ElementMatch<CodeInstruction>("{any branch to `renderDroneEndCapLabel`}", i => ILMatches.AnyBranch.Matches(i) && Equals(i.operand, renderDroneEndCapLabel))],
					matcher => matcher
						.PointerMatcher(SequenceMatcherRelativeElement.First)
						.Element(out var instruction)
						.Replace(new CodeInstruction(instruction.opcode, trueRenderDroneEndCapLabel))
						.BlockMatcher()
				)
				.ForEach(
					SequenceMatcherRelativeBounds.Enclosed,
					[new ElementMatch<CodeInstruction>("{any branch to `pastRenderDroneEndCapLabel`}", i => ILMatches.AnyBranch.Matches(i) && Equals(i.operand, pastRenderDroneEndCapLabel))],
					matcher => matcher
						.PointerMatcher(SequenceMatcherRelativeElement.First)
						.Element(out var instruction)
						.Replace(new CodeInstruction(instruction.opcode, falseRenderDroneEndCapLabel))
						.BlockMatcher()
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static bool Combat_DrawIntentLinesForPart_Transpiler_ModifyRendering(Combat combat, Ship shipSource, int partIndex, Part part, bool renderDroneEndCap, ref Color highlightColor)
	{
		if (MG.inst.g.state.route != combat)
			return renderDroneEndCap;
		
		var args = ModEntry.Instance.ArgsPool.Get<ApiImplementation.V2Api.AttackLogicApi.ModifyHighlightRenderingArgs>();
		try
		{
			args.State = MG.inst.g.state;
			args.Combat = combat;
			args.Ship = shipSource;
			args.Part = part;
			args.WorldX = shipSource.x + partIndex;
			args.Object = combat.stuff.GetValueOrDefault(shipSource.x + partIndex);
			args.HighlightColor = highlightColor;
			args.StopsInMidrow = renderDroneEndCap;

			foreach (var hook in Instance.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, MG.inst.g.state.EnumerateAllArtifacts()))
				if (hook.ModifyHighlightRendering(args))
					break;

			highlightColor = args.HighlightColor;
			return args.StopsInMidrow;
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}
}