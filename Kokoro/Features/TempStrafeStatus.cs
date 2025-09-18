using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class V2Api
	{
		public IKokoroApi.IV2.ITempStrafeStatusApi TempStrafeStatus { get; } = new TempStrafeStatusApi();
		
		public sealed class TempStrafeStatusApi : IKokoroApi.IV2.ITempStrafeStatusApi
		{
			public Status Status
				=> Instance.Content.TempStrafeStatus.Status;
		}
	}
}

internal sealed class TempStrafeStatusManager : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	internal static readonly TempStrafeStatusManager Instance = new();

	private TempStrafeStatusManager()
	{
	}

	internal static void SetupLate(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AMove), nameof(AMove.Begin)),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AMove_Begin_Transpiler)), priority: Priority.VeryLow)
		);
	}

	private static IEnumerable<CodeInstruction> AMove_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.ForEach(
					SequenceMatcherRelativeBounds.WholeSequence,
					[
						ILMatches.Ldloc<Ship>(originalMethod).GetLocalIndex(out var shipLocalIndex),
						ILMatches.LdcI4(Status.strafe),
						ILMatches.Call("Get"),
					],
					matcher => matcher
						.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
							new CodeInstruction(OpCodes.Ldloc, shipLocalIndex.Value),
							new CodeInstruction(OpCodes.Ldc_I4, (int)ModEntry.Instance.Content.TempStrafeStatus.Status),
							new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Get))),
							new CodeInstruction(OpCodes.Add),
						]),
					minExpectedOccurences: 2, maxExpectedOccurences: 2
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}
	
	public IReadOnlySet<Status> GetStatusesToCallTurnTriggerHooksFor(IKokoroApi.IV2.IStatusLogicApi.IHook.IGetStatusesToCallTurnTriggerHooksForArgs args)
		=> args.NonZeroStatuses.Contains(ModEntry.Instance.Content.TempStrafeStatus.Status) ? new HashSet<Status> { ModEntry.Instance.Content.TempStrafeStatus.Status } : [];

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;

		args.Amount = 0;
		return false;
	}
}
