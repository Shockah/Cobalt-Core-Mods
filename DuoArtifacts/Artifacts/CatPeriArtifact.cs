using System.Linq;

namespace Shockah.DuoArtifacts;

internal sealed class CatPeriArtifact : DuoArtifact, IStatusLogicHook
{
	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		var data = card.GetDataWithOverrides(state);
		if (!data.temporary)
			return;
		if (!card.GetActions(state, combat).Any(a => a is AAttack))
			return;

		combat.QueueImmediate(new AStatus
		{
			status = Status.overdrive,
			statusAmount = 1,
			targetPlayer = true
		});
		Pulse();
	}

	public bool HandleStatusTurnAutoStep(State state, Combat combat, StatusTurnTriggerTiming timing, Ship ship, Status status, ref int amount, ref StatusTurnAutoStepSetStrategy setStrategy)
	{
		if (timing == StatusTurnTriggerTiming.TurnEnd && status == Status.overdrive && amount > 0)
			amount--;
		Pulse();
		return false;
	}
}