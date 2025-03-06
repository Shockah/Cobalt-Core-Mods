using HarmonyLib;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	partial class ActionApiImplementation
	{
		public ACardOffering WithDestination(ACardOffering action, CardDestination? destination, bool? insertRandomly = null)
		{
			var copy = Mutil.DeepCopy(action);
			Instance.Helper.ModData.SetOptionalModData(copy, "destination", destination);
			Instance.Helper.ModData.SetOptionalModData(copy, "destinationInsertRandomly", insertRandomly);
			return copy;
		}

		public CardReward WithDestination(CardReward route, CardDestination? destination, bool? insertRandomly = null)
		{
			var copy = Mutil.DeepCopy(route);
			Instance.Helper.ModData.SetOptionalModData(copy, "destination", destination);
			Instance.Helper.ModData.SetOptionalModData(copy, "destinationInsertRandomly", insertRandomly);
			return copy;
		}
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.ICardDestinationApi CardDestination { get; } = new CardDestinationApi();
		
		public sealed class CardDestinationApi : IKokoroApi.IV2.ICardDestinationApi
		{
			public IKokoroApi.IV2.ICardDestinationApi.ICardOffering ModifyCardOffering(ACardOffering action)
			{
				var wrapped = Mutil.DeepCopy(action);
				return new Wrapper { Wrapped = wrapped };
			}

			public IKokoroApi.IV2.ICardDestinationApi.ICardReward ModifyCardReward(CardReward route)
			{
				var wrapped = Mutil.DeepCopy(route);
				return new RouteWrapper { Wrapped = wrapped };
			}

			private sealed class Wrapper : IKokoroApi.IV2.ICardDestinationApi.ICardOffering
			{
				public required ACardOffering Wrapped { get; init; }

				[JsonIgnore]
				public ACardOffering AsCardAction
					=> Wrapped;

				public CardDestination? Destination
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<CardDestination>(Wrapped, "destination");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "destination", value);
				}
				
				public bool? InsertRandomly
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "destinationInsertRandomly");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "destinationInsertRandomly", value);
				}
				
				public IKokoroApi.IV2.ICardDestinationApi.ICardOffering SetDestination(CardDestination? value)
				{
					Destination = value;
					return this;
				}

				public IKokoroApi.IV2.ICardDestinationApi.ICardOffering SetInsertRandomly(bool? value)
				{
					InsertRandomly = value;
					return this;
				}
			}
			
			private sealed class RouteWrapper : IKokoroApi.IV2.ICardDestinationApi.ICardReward
			{
				public required CardReward Wrapped { get; init; }

				[JsonIgnore]
				public CardReward AsRoute
					=> Wrapped;

				public CardDestination? Destination
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<CardDestination>(Wrapped, "destination");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "destination", value);
				}
				
				public bool? InsertRandomly
				{
					get => ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(Wrapped, "destinationInsertRandomly");
					set => ModEntry.Instance.Helper.ModData.SetOptionalModData(Wrapped, "destinationInsertRandomly", value);
				}
				
				public IKokoroApi.IV2.ICardDestinationApi.ICardReward SetDestination(CardDestination? value)
				{
					Destination = value;
					return this;
				}

				public IKokoroApi.IV2.ICardDestinationApi.ICardReward SetInsertRandomly(bool? value)
				{
					InsertRandomly = value;
					return this;
				}
			}
		}
	}
}

internal sealed class CardDestinationManager
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
				card.ExhaustFX();
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
		
		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "destination", ModEntry.Instance.Helper.ModData.GetOptionalModData<CardDestination>(__instance, "destination"));
		ModEntry.Instance.Helper.ModData.SetOptionalModData(route, "destinationInsertRandomly", ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "destinationInsertRandomly"));
	}
	
	private static bool CardReward_TakeCard_Prefix(CardReward __instance, G g, Card card)
	{
		if (g.state.route is Combat combat)
		{
			var destination = ModEntry.Instance.Helper.ModData.GetOptionalModData<CardDestination>(__instance, "destination") ?? CardDestination.Hand;
			var insertRandomly = ModEntry.Instance.Helper.ModData.GetOptionalModData<bool>(__instance, "destinationInsertRandomly");

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