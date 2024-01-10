using HarmonyLib;
using Shockah.Shared;

namespace Shockah.Dracula;

internal sealed class NegativeOverdriveManager : IStatusLogicHook
{
	public NegativeOverdriveManager()
	{
		ModEntry.Instance.KokoroApi.RegisterStatusLogicHook(this, 0);

		ModEntry.Instance.Harmony.TryPatch(
			logger: ModEntry.Instance.Logger,
			original: () => AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.CanBeNegative)),
			postfix: new HarmonyMethod(GetType(), nameof(Ship_CanBeNegative_Postfix))
		);
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (status != Status.overdrive)
			return false;
		if (timing != StatusTurnTriggerTiming.TurnEnd)
			return false;

		if (amount < 0)
			amount++;
		return true;
	}

	private static void Ship_CanBeNegative_Postfix(Status status, ref bool __result)
	{
		if (status == Status.overdrive)
			__result = true;
	}
}
