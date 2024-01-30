using System;
using System.Linq;

namespace Shockah.Johnson;

internal sealed class CrunchTimeManager
{
	public CrunchTimeManager()
	{
		ModEntry.Instance.Helper.Events.RegisterBeforeArtifactsHook(nameof(Artifact.OnTurnStart), (State state, Combat combat) =>
		{
			if (!combat.isPlayerTurn)
				return;

			var stacks = state.ship.Get(ModEntry.Instance.CrunchTimeStatus.Status);
			if (stacks <= 0)
				return;

			var countInHand = combat.hand.Count(card => card is BurnOutCard);
			if (countInHand >= 3)
				return;

			combat.Queue(new AAddCard
			{
				destination = CardDestination.Hand,
				card = new BurnOutCard(),
				amount = stacks,
				statusPulse = ModEntry.Instance.CrunchTimeStatus.Status
			});
		}, 0);
	}
}
