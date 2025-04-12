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
		public IKokoroApi.IV2.IDriveStatusApi DriveStatus { get; } = new DriveStatusApi();
		
		public sealed class DriveStatusApi : IKokoroApi.IV2.IDriveStatusApi
		{
			public Status Underdrive
				=> (Status)ModEntry.Instance.Content.UnderdriveStatus.Id!.Value;
			
			public Status Pulsedrive
				=> (Status)ModEntry.Instance.Content.PulsedriveStatus.Id!.Value;
			
			public Status Minidrive
				=> (Status)ModEntry.Instance.Content.MinidriveStatus.Id!.Value;
		}
	}
}

internal sealed class DriveStatusManager : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	internal static readonly DriveStatusManager Instance = new();

	private DriveStatusManager()
	{
	}

	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetActualDamage)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetActualDamage_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> Card_GetActualDamage_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<int>(originalMethod).CreateLdlocaInstruction(out var ldlocaDamage),
					ILMatches.Ldloc<Ship>(originalMethod).CreateLdlocInstruction(out var ldlocShip),
					ILMatches.LdcI4((int)Status.powerdrive),
					ILMatches.Call("Get"),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.Stloc<int>(originalMethod),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					ldlocShip,
					ldlocaDamage,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType, nameof(Card_GetActualDamage_Transpiler_ModifyDamage))),
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

	private static void Card_GetActualDamage_Transpiler_ModifyDamage(Ship ship, ref int damage)
	{
		damage -= ship.Get((Status)ModEntry.Instance.Content.UnderdriveStatus.Id!.Value);
		damage += ship.Get((Status)ModEntry.Instance.Content.PulsedriveStatus.Id!.Value);
		if (ship.Get((Status)ModEntry.Instance.Content.MinidriveStatus.Id!.Value) > 0)
			damage++;
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd)
			return false;

		if (args.Status == (Status)ModEntry.Instance.Content.UnderdriveStatus.Id!.Value)
			args.Amount--;
		else if (args.Status == (Status)ModEntry.Instance.Content.MinidriveStatus.Id!.Value)
			args.Amount--;
		else if (args.Status == (Status)ModEntry.Instance.Content.PulsedriveStatus.Id!.Value)
			args.Amount = 0;
		return false;
	}
}
