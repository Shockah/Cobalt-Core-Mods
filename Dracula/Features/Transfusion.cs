using HarmonyLib;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

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
			prefix: new HarmonyMethod(GetType(), nameof(Combat_Update_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Combat_Update_Postfix))
		);

		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnCombatEnd), (State state) =>
		{
			var transfusion = state.ship.Get(ModEntry.Instance.TransfusionStatus.Status);
			var transfusing = state.ship.Get(ModEntry.Instance.TransfusingStatus.Status);
			var toHeal = Math.Min(transfusion, transfusing);
			if (toHeal <= 0)
				return;

			(state.route as Combat)?.QueueImmediate(new AHeal
			{
				targetPlayer = true,
				healAmount = toHeal,
				canRunAfterKill = true
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

		combat.QueueImmediate(new AStatus
		{
			targetPlayer = ship.isPlayerShip,
			status = ModEntry.Instance.TransfusingStatus.Status,
			statusAmount = 1
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

	private static void Combat_Update_Prefix(Combat __instance, G g, ref ((int Progress, int Total) Player, (int Progress, int Total) Enemy) __state)
		=> __state = (
			Player: (
				Progress: g.state.ship.Get(ModEntry.Instance.TransfusingStatus.Status),
				Total: g.state.ship.Get(ModEntry.Instance.TransfusionStatus.Status)
			),
			Enemy: (
				Progress: __instance.otherShip.Get(ModEntry.Instance.TransfusingStatus.Status),
				Total: __instance.otherShip.Get(ModEntry.Instance.TransfusionStatus.Status)
			)
		);

	private static void Combat_Update_Postfix(Combat __instance, G g, ref ((int Progress, int Total) Player, (int Progress, int Total) Enemy) __state)
	{
		var newState = (
			Player: (
				Progress: g.state.ship.Get(ModEntry.Instance.TransfusingStatus.Status),
				Total: g.state.ship.Get(ModEntry.Instance.TransfusionStatus.Status)
			),
			Enemy: (
				Progress: __instance.otherShip.Get(ModEntry.Instance.TransfusingStatus.Status),
				Total: __instance.otherShip.Get(ModEntry.Instance.TransfusionStatus.Status)
			)
		);

		void DoForShip(Ship ship, (int Progress, int Total) oldState, (int Progress, int Total) newState)
		{
			if (!(newState.Progress >= newState.Total && (newState.Progress != oldState.Progress || newState.Total != oldState.Total)))
				return;

			if (newState.Total > 0)
				__instance.QueueImmediate(new AHeal
				{
					targetPlayer = ship.isPlayerShip,
					healAmount = newState.Total
				});
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

		DoForShip(g.state.ship, __state.Player, newState.Player);
		DoForShip(__instance.otherShip, __state.Enemy, newState.Enemy);
	}
}
