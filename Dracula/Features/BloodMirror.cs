﻿using HarmonyLib;
using Shockah.Kokoro;

namespace Shockah.Dracula;

internal static class BloodMirrorExt
{
	public static int GetBloodMirrorDepth(this AHurt self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<int>(self, "BloodMirrorDepth");

	public static void SetBloodMirrorDepth(this AHurt self, int value)
		=> ModEntry.Instance.Helper.ModData.SetModData(self, "BloodMirrorDepth", value);
}

internal sealed class BloodMirrorManager : IKokoroApi.IV2.IStatusLogicApi.IHook
{
	private static int BloodMirrorDepth;

	public BloodMirrorManager()
	{
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(this);
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AHurt), nameof(AHurt.Begin)),
			prefix: new HarmonyMethod(GetType(), nameof(AHurt_Begin_Prefix)),
			finalizer: new HarmonyMethod(GetType(), nameof(AHurt_Begin_Finalizer))
		);
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args.Status != ModEntry.Instance.BloodMirrorStatus.Status)
			return false;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return false;

		if (args.Amount > 0)
			args.Amount--;
		return false;
	}

	private static void Ship_DirectHullDamage_Prefix(Ship __instance, out int __state)
		=> __state = __instance.hull;

	private static void Ship_DirectHullDamage_Postfix(Ship __instance, Combat c, ref int __state)
	{
		var damageTaken = __state - __instance.hull;

		if (damageTaken <= 0)
			return;
		if (BloodMirrorDepth >= 2)
			return;
		if (__instance.Get(ModEntry.Instance.BloodMirrorStatus.Status) <= 0)
			return;

		var action = new AHurt
		{
			targetPlayer = !__instance.isPlayerShip,
			hurtAmount = damageTaken * 2,
			statusPulse = ModEntry.Instance.BloodMirrorStatus.Status
		};
		action.SetBloodMirrorDepth(BloodMirrorDepth + 1);
		c.QueueImmediate(action);
	}

	private static void AHurt_Begin_Prefix(AHurt __instance)
		=> BloodMirrorDepth = __instance.GetBloodMirrorDepth();

	private static void AHurt_Begin_Finalizer()
		=> BloodMirrorDepth = 0;
}
