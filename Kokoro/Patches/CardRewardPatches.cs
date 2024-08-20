using HarmonyLib;
using Nickel;
using System;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

// ReSharper disable InconsistentNaming
internal static class CardRewardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.TakeCard)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_TakeCard_Prefix)), priority: Priority.First)
		);
	}

	private static void SendCardToDestination(State state, Combat combat, Card card, CardDestination destination, bool? insertRandomly)
	{
		switch (destination)
		{
			case CardDestination.Deck:
				state.SendCardToDeck(card, doAnimation: true, insertRandomly ?? true);
				break;
			case CardDestination.Hand:
				combat.SendCardToHand(
					state,
					card,
					(insertRandomly ?? false)
						? (combat.hand.Count == 0 ? null : state.rngActions.NextInt() % combat.hand.Count)
						: null
				);
				break;
			case CardDestination.Discard:
				combat.SendCardToDiscard(state, card);
				break;
			case CardDestination.Exhaust:
				combat.SendCardToExhaust(state, card);
				break;
			default:
				throw new ArgumentException();
		}
	}

	private static bool CardReward_TakeCard_Prefix(CardReward __instance, G g, Card card)
	{
		if (g.state.route is Combat combat)
		{
			var destination = Instance.Api.TryGetExtensionData(__instance, "destination", out CardDestination modDataDestination) ? modDataDestination : CardDestination.Hand;
			bool? insertRandomly = Instance.Api.TryGetExtensionData(__instance, "destinationInsertRandomly", out bool modDataDestinationInsertRandomly) ? modDataDestinationInsertRandomly : null;

			SendCardToDestination(g.state, combat, card, destination, insertRandomly);
			foreach (var artifact in g.state.EnumerateAllArtifacts())
				artifact.OnPlayerRecieveCardMidCombat(g.state, combat, card);
		}
		else
		{
			Analytics.Log(g.state, "cardReward", new
			{
				card = card.Key(),
				cards = __instance.cards.Select(c => c.Key())
			});
			g.state.SendCardToDeck(card, doAnimation: true, insertRandomly: true);
		}

		card.targetPos = new Vec(G.screenSize.x / 2, G.screenSize.y + 100);
		__instance.takeCardAnimation = 0;

		return false;
	}
}