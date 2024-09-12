using HarmonyLib;
using Nickel;
using System;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	partial class ActionApiImplementation
	{
		public ACardOffering WithDestination(ACardOffering action, CardDestination? destination, bool? insertRandomly = null)
		{
			var copy = Mutil.DeepCopy(action);

			if (destination is null)
				Instance.Api.RemoveExtensionData(copy, "destination");
			else
				Instance.Api.SetExtensionData(copy, "destination", destination.Value);

			if (insertRandomly is null)
				Instance.Api.RemoveExtensionData(copy, "destinationInsertRandomly");
			else
				Instance.Api.SetExtensionData(copy, "destinationInsertRandomly", insertRandomly.Value);

			return copy;
		}

		public CardReward WithDestination(CardReward route, CardDestination? destination, bool? insertRandomly = null)
		{
			var copy = Mutil.DeepCopy(route);

			if (destination is null)
				Instance.Api.RemoveExtensionData(copy, "destination");
			else
				Instance.Api.SetExtensionData(copy, "destination", destination.Value);

			if (insertRandomly is null)
				Instance.Api.RemoveExtensionData(copy, "destinationInsertRandomly");
			else
				Instance.Api.SetExtensionData(copy, "destinationInsertRandomly", insertRandomly.Value);

			return copy;
		}
	}
}

internal sealed class CardOfferingAndRewardDestinationManager
{
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ACardOffering), nameof(ACardOffering.BeginWithRoute)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ACardOffering_BeginWithRoute_Postfix))
		);
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
	
	private static void ACardOffering_BeginWithRoute_Postfix(ACardOffering __instance, Route? __result)
	{
		if (__result is not CardReward route)
			return;

		if (ModEntry.Instance.Api.TryGetExtensionData(__instance, "destination", out CardDestination destination))
			ModEntry.Instance.Api.SetExtensionData(route, "destination", destination);
		if (ModEntry.Instance.Api.TryGetExtensionData(__instance, "destinationInsertRandomly", out bool destinationInsertRandomly))
			ModEntry.Instance.Api.SetExtensionData(route, "destinationInsertRandomly", destinationInsertRandomly);
	}
	
	private static bool CardReward_TakeCard_Prefix(CardReward __instance, G g, Card card)
	{
		if (g.state.route is Combat combat)
		{
			var destination = ModEntry.Instance.Api.TryGetExtensionData(__instance, "destination", out CardDestination modDataDestination) ? modDataDestination : CardDestination.Hand;
			bool? insertRandomly = ModEntry.Instance.Api.TryGetExtensionData(__instance, "destinationInsertRandomly", out bool modDataDestinationInsertRandomly) ? modDataDestinationInsertRandomly : null;

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