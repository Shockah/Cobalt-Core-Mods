using HarmonyLib;
using Newtonsoft.Json;
using Shockah.Shared;
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

internal sealed class TransfusionManager : IStatusLogicHook, IStatusRenderHook
{
	public TransfusionManager()
	{
		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(this, 0);
		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(this, 0);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Postfix))
		);
		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.Update)),
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

	public void OnStatusTurnTrigger(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount)
	{
		if (status != ModEntry.Instance.TransfusionStatus.Status)
			return;
		if (timing != StatusTurnTriggerTiming.TurnStart)
			return;
		if (oldAmount <= 0)
			return;

		var thinBloodArtifact = state.EnumerateAllArtifacts().FirstOrDefault(a => a is ThinBloodArtifact);
		var triggers = thinBloodArtifact is null ? 1 : 2;
		combat.QueueImmediate(new AStatus
		{
			targetPlayer = ship.isPlayerShip,
			status = ModEntry.Instance.TransfusingStatus.Status,
			statusAmount = triggers,
			artifactPulse = thinBloodArtifact?.Key()
		});
	}

	public bool? ShouldShowStatus(State state, Combat combat, Ship ship, Status status, int amount)
		=> status == ModEntry.Instance.TransfusingStatus.Status ? false : null;

	public bool? ShouldOverrideStatusRenderingAsBars(State state, Combat combat, Ship ship, Status status, int amount)
		=> status == ModEntry.Instance.TransfusionStatus.Status ? true : null;

	public (IReadOnlyList<Color> Colors, int? BarTickWidth) OverrideStatusRendering(State state, Combat combat, Ship ship, Status status, int amount)
	{
		if (status != ModEntry.Instance.TransfusionStatus.Status)
			return ([], null);
		var transfusing = Math.Min(ship.Get(ModEntry.Instance.TransfusingStatus.Status), amount);
		return (
			Colors: Enumerable.Range(0, transfusing).Select(_ => ModEntry.Instance.KokoroApi.DefaultActiveStatusBarColor)
				.Concat(Enumerable.Range(0, amount - transfusing).Select(_ => ModEntry.Instance.KokoroApi.DefaultInactiveStatusBarColor))
				.ToList(),
			BarTickWidth: null
		);
	}

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, bool isForShipStatus, List<Tooltip> tooltips)
	{
		if (status != ModEntry.Instance.TransfusionStatus.Status)
			return tooltips;
		return tooltips.Concat(StatusMeta.GetTooltips(ModEntry.Instance.TransfusingStatus.Status, 1)).ToList();
	}

	private static void Ship_DirectHullDamage_Prefix(Ship __instance, ref int __state)
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
		void DoForShip(Ship ship)
		{
			var progress = ship.Get(ModEntry.Instance.TransfusingStatus.Status);
			var total = ship.Get(ModEntry.Instance.TransfusionStatus.Status);

			if (progress <= 0 || total <= 0 || progress < total)
				return;
			if (ship.IsTransfusionDisabled())
				return;

			ship.SetTransfusionDisabled(true);
			__instance.QueueImmediate(new AReenableTransfusion
			{
				TargetPlayer = ship.isPlayerShip,
				canRunAfterKill = true
			});
			if (total > 0)
			{
				__instance.QueueImmediate(new AHeal
				{
					targetPlayer = ship.isPlayerShip,
					healAmount = total,
					canRunAfterKill = true
				});
			}
			__instance.QueueImmediate(new AStatus
			{
				targetPlayer = ship.isPlayerShip,
				mode = AStatusMode.Set,
				status = ModEntry.Instance.TransfusionStatus.Status,
				statusAmount = 0
			});
			__instance.QueueImmediate(new AStatus
			{
				targetPlayer = ship.isPlayerShip,
				mode = AStatusMode.Set,
				status = ModEntry.Instance.TransfusingStatus.Status,
				statusAmount = 0
			});
		}

		DoForShip(g.state.ship);
		DoForShip(__instance.otherShip);
	}

	public sealed class AReenableTransfusion : CardAction
	{
		[JsonProperty]
		public bool TargetPlayer = false;

		public override void Begin(G g, State s, Combat c)
		{
			base.Begin(g, s, c);
			timer = 0;
			var ship = TargetPlayer ? s.ship : c.otherShip;
			ship.SetTransfusionDisabled(false);
		}
	}
}
