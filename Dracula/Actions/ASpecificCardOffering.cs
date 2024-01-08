using HarmonyLib;
using Microsoft.Extensions.Logging;
using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

public sealed class ASpecificCardOffering : CardAction
{
	public enum CardTargetLocation
	{
		Hand, Deck, Discard, Exhaust
	}

	public List<Card> Cards { get; set; } = [];
	public bool CanSkip { get; set; } = false;
	public CardTargetLocation TargetLocation { get; set; } = CardTargetLocation.Hand;

	internal static void ApplyPatches(Harmony harmony, ILogger logger)
	{
		harmony.TryPatch(
			logger: logger,
			original: () => AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.TakeCard)),
			postfix: new HarmonyMethod(typeof(ASpecificCardOffering), nameof(CardReward_TakeCard_Postfix))
		);
	}

	public override Route? BeginWithRoute(G g, State s, Combat c)
	{
		timer = 0;
		return new CustomCardReward
		{
			cards = Cards.Select(c =>
			{
				c.drawAnim = 1;
				c.flipAnim = 1;
				return c;
			}).ToList(),
			canSkip = CanSkip,
			TargetLocation = TargetLocation
		};
	}

	public override List<Tooltip> GetTooltips(State s)
		=> [
			new TTGlossary("action.cardOffering")
		];

	private static void CardReward_TakeCard_Postfix(CardReward __instance, G g, Card card)
	{
		if (__instance is not CustomCardReward custom)
			return;
		if (g.state.route is not Combat combat)
			return;

		g.state.RemoveCardFromWhereverItIs(card.uuid);
		switch (custom.TargetLocation)
		{
			case CardTargetLocation.Hand:
				combat.SendCardToHand(g.state, card);
				break;
			case CardTargetLocation.Deck:
				g.state.SendCardToDeck(card);
				break;
			case CardTargetLocation.Discard:
				combat.SendCardToDiscard(g.state, card);
				break;
			case CardTargetLocation.Exhaust:
				combat.SendCardToExhaust(g.state, card);
				break;
		}
	}

	public sealed class CustomCardReward : CardReward
	{
		public CardTargetLocation TargetLocation { get; set; } = CardTargetLocation.Hand;
	}
}
