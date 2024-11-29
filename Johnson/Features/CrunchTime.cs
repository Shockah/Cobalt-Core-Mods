using Shockah.Kokoro;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Johnson;

internal sealed class CrunchTimeManager : IKokoroApi.IV2.IStatusRenderingApi.IHook
{
	public CrunchTimeManager()
	{
		ModEntry.Instance.KokoroApi.StatusRendering.RegisterHook(this);

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
		});
	}

	public IReadOnlyList<Tooltip> OverrideStatusTooltips(IKokoroApi.IV2.IStatusRenderingApi.IHook.IOverrideStatusTooltipsArgs args)
	{
		if (args.Status != ModEntry.Instance.CrunchTimeStatus.Status)
			return args.Tooltips;
		return args.Tooltips
			.Append(new TTCard { card = new BurnOutCard() })
			.ToList();
	}
}
