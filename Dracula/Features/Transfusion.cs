using HarmonyLib;
using Newtonsoft.Json;
using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

internal static class TransfusionExt
{
	public static bool IsTransfusionDisabled(this Ship self)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(self, "IsTransfusionDisabled");

	public static void SetTransfusionDisabled(this Ship self, bool value)
		=> ModEntry.Instance.Helper.ModData.SetModData(self, "IsTransfusionDisabled", value);
}

internal sealed class TransfusionManager : IKokoroApi.IV2.IStatusLogicApi.IHook, IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	public TransfusionManager()
	{
		ModEntry.Instance.KokoroApi.StatusLogic.RegisterHook(this);
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(this);

		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Postfix))
		);
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_Update_Postfix))
		);

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			state.ship.SetTransfusionDisabled(false);
			var transfusion = state.ship.Get(ModEntry.Instance.TransfusionStatus.Status);
			var transfusing = state.ship.Get(ModEntry.Instance.TransfusingStatus.Status);
			var toHeal = Math.Min(transfusion, transfusing);
			if (toHeal <= 0)
				return;

			state.rewardsQueue.QueueImmediate(new AHeal
			{
				targetPlayer = true,
				healAmount = toHeal
			});
		}, priority: 0);
	}

	public void OnStatusTurnTrigger(IKokoroApi.IV2.IStatusLogicApi.IHook.IOnStatusTurnTriggerArgs args)
	{
		if (args.Status != ModEntry.Instance.TransfusionStatus.Status)
			return;
		if (args.Timing != IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnStart)
			return;
		if (args.OldAmount <= 0)
			return;

		var progress = args.Ship.Get(ModEntry.Instance.TransfusingStatus.Status);
		var thinBloodArtifact = args.State.EnumerateAllArtifacts().FirstOrDefault(a => a is ThinBloodArtifact);
		var triggers = Math.Min(thinBloodArtifact is null ? 1 : 2, args.OldAmount - progress);

		args.Combat.QueueImmediate(new AStatus
		{
			targetPlayer = args.Ship.isPlayerShip,
			status = ModEntry.Instance.TransfusingStatus.Status,
			statusAmount = triggers,
			artifactPulse = triggers > 1 ? thinBloodArtifact?.Key() : null,
		});
	}

	public bool? ShouldShowStatus(IKokoroApi.IV2.IStatusRenderingApi.IHook.IShouldShowStatusArgs args)
		=> args.Status == ModEntry.Instance.TransfusingStatus.Status ? false : null;

	public (IReadOnlyList<Color> Colors, int? BarTickWidth)? OverrideStatusRenderingAsBars(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusRenderingAsBarsArgs args)
	{
		if (args.Status != ModEntry.Instance.TransfusionStatus.Status)
			return null;
		
		var transfusing = Math.Min(args.Ship.Get(ModEntry.Instance.TransfusingStatus.Status), args.Amount);
		return (
			Colors: Enumerable.Range(0, transfusing).Select(_ => ModEntry.Instance.KokoroApi.StatusRendering.DefaultActiveStatusBarColor)
				.Concat(Enumerable.Range(0, args.Amount - transfusing).Select(_ => ModEntry.Instance.KokoroApi.StatusRendering.DefaultInactiveStatusBarColor))
				.ToList(),
			BarTickWidth: null
		);
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		if (args.Status != ModEntry.Instance.TransfusionStatus.Status)
			return args.Tooltips;
		return args.Tooltips.Concat(StatusMeta.GetTooltips(ModEntry.Instance.TransfusingStatus.Status, 1)).ToList();
	}

	private static void Ship_DirectHullDamage_Prefix(Ship __instance, out int __state)
		=> __state = __instance.hull;

	private static void Ship_DirectHullDamage_Postfix(Ship __instance, Combat c, ref int __state)
	{
		var damageTaken = __state - __instance.hull;
		if (damageTaken <= 0)
			return;
		if (__instance.Get(ModEntry.Instance.TransfusionStatus.Status) <= 0)
			return;

		c.QueueImmediate(new AStatus
		{
			targetPlayer = __instance.isPlayerShip,
			status = ModEntry.Instance.TransfusionStatus.Status,
			statusAmount = -damageTaken
		});
	}

	private static void Combat_Update_Postfix(Combat __instance, G g)
	{
		DoForShip(g.state.ship);
		DoForShip(__instance.otherShip);

		void DoForShip(Ship ship)
		{
			var progress = ship.Get(ModEntry.Instance.TransfusingStatus.Status);
			var total = ship.Get(ModEntry.Instance.TransfusionStatus.Status);
			var missingHull = ship.hullMax - ship.hull;
			var healingRequiredForMax = Math.Max(missingHull - (ship.isPlayerShip ? g.state.EnumerateAllArtifacts().Sum(a => a.ModifyHealAmount(progress, g.state, true)) : 0), 0);
			var wouldHealToFullNow = progress >= healingRequiredForMax;

			if (progress <= 0)
				return;
			if (progress < total && (!wouldHealToFullNow || ship.hull >= ship.hullMax))
				return;
			if (ship.IsTransfusionDisabled())
				return;

			ship.SetTransfusionDisabled(true);
			__instance.QueueImmediate(new AReenableTransfusion
			{
				TargetPlayer = ship.isPlayerShip,
				canRunAfterKill = true,
			});
			__instance.QueueImmediate(new AHeal
			{
				targetPlayer = ship.isPlayerShip,
				healAmount = progress,
				canRunAfterKill = true,
			});
			__instance.QueueImmediate(new AStatus
			{
				targetPlayer = ship.isPlayerShip,
				mode = AStatusMode.Set,
				status = ModEntry.Instance.TransfusionStatus.Status,
				statusAmount = 0,
				timer = 0,
			});
			__instance.QueueImmediate(new AStatus
			{
				targetPlayer = ship.isPlayerShip,
				mode = AStatusMode.Set,
				status = ModEntry.Instance.TransfusingStatus.Status,
				statusAmount = 0,
				timer = 0,
			});
		}
	}

	public sealed class AReenableTransfusion : CardAction
	{
		[JsonProperty]
		public bool TargetPlayer;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			var ship = TargetPlayer ? s.ship : c.otherShip;
			ship.SetTransfusionDisabled(false);
		}
	}
}
