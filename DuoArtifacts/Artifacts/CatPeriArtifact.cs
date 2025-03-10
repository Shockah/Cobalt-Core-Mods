﻿using System.Linq;
using Shockah.Kokoro;

namespace Shockah.DuoArtifacts;

internal sealed class CatPeriArtifact : DuoArtifact, IKokoroApi.IV2.IStatusLogicApi.IHook
{
	public override void OnPlayerPlayCard(int energyCost, Deck deck, Card card, State state, Combat combat, int handPosition, int handCount)
	{
		base.OnPlayerPlayCard(energyCost, deck, card, state, combat, handPosition, handCount);
		if (!ModEntry.Instance.Helper.Content.Cards.IsCardTraitActive(state, card, ModEntry.Instance.Helper.Content.Cards.TemporaryCardTrait))
			return;
		if (!card.GetActions(state, combat).Any(a => a is AAttack))
			return;

		combat.QueueImmediate(new AStatus
		{
			status = Status.overdrive,
			statusAmount = 1,
			targetPlayer = true,
			artifactPulse = Key(),
		});
	}

	public bool HandleStatusTurnAutoStep(IKokoroApi.IV2.IStatusLogicApi.IHook.IHandleStatusTurnAutoStepArgs args)
	{
		if (args is { Timing: IKokoroApi.IV2.IStatusLogicApi.StatusTurnTriggerTiming.TurnEnd, Status: Status.overdrive, Amount: > 0 })
			args.Amount--;
		Pulse();
		return false;
	}
}