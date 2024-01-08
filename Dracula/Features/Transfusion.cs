using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Dracula;

internal sealed class TransfusionManager : IStatusLogicHook
{
	public TransfusionManager()
	{
		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(this, 0);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.DirectHullDamage)),
			prefix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Prefix)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_DirectHullDamage_Postfix))
		);
	}

	public void OnStatusTurnTrigger(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, int oldAmount, int newAmount)
	{
		if (status != ModEntry.Instance.TransfusionStatus.Status)
			return;
		if (timing != StatusTurnTriggerTiming.TurnStart)
			return;
		if (oldAmount <= 0)
			return;

		combat.QueueImmediate(new AHeal
		{
			targetPlayer = ship.isPlayerShip,
			healAmount = oldAmount
		});
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (status != ModEntry.Instance.TransfusionStatus.Status)
			return false;
		if (timing != StatusTurnTriggerTiming.TurnStart)
			return false;

		amount = 0;
		return false;
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
}
