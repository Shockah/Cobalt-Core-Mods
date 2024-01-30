using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Johnson;

internal sealed class CrunchTimeManager : IStatusRenderHook
{
	public CrunchTimeManager()
	{
		ModEntry.Instance.KokoroApi.RegisterStatusRenderHook(this, 0);

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

	public List<Tooltip> OverrideStatusTooltips(Status status, int amount, Ship? ship, List<Tooltip> tooltips)
	{
		if (status != ModEntry.Instance.CrunchTimeStatus.Status)
			return tooltips;
		return tooltips
			.Concat(new BurnOutCard().GetAllTooltips(MG.inst.g, DB.fakeState))
			.ToList();
	}
}
