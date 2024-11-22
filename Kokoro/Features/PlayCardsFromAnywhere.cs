using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	partial class ActionApiImplementation
	{
		public CardAction MakePlaySpecificCardFromAnywhere(int cardId, bool showTheCardIfNotInHand = true)
			=> new APlaySpecificCardFromAnywhere { CardIds = [cardId], ShowTheCardIfNotInHand = showTheCardIfNotInHand };

		public CardAction MakePlayRandomCardsFromAnywhere(IEnumerable<int> cardIds, int amount = 1, bool showTheCardIfNotInHand = true)
			=> new APlayRandomCardsFromAnywhere { CardIds = cardIds.ToHashSet(), Amount = amount, ShowTheCardIfNotInHand = showTheCardIfNotInHand };
	}
	
	#endregion
	
	partial class V2Api
	{
		public IKokoroApi.IV2.IPlayCardsFromAnywhereApi PlayCardsFromAnywhere { get; } = new PlayCardsFromAnywhereApi();
		
		public sealed class PlayCardsFromAnywhereApi : IKokoroApi.IV2.IPlayCardsFromAnywhereApi
		{
			public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction? AsAction(CardAction action)
				=> action as IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction;

			public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction MakeAction(int cardId)
				=> new APlaySpecificCardFromAnywhere { CardIds = [cardId] };

			public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction MakeAction(IEnumerable<int> cardIds, int amount = 1)
				=> new APlayRandomCardsFromAnywhere { CardIds = cardIds.ToHashSet(), Amount = amount };
		}
	}
}

internal sealed class PlaySpecificCardFromAnywhereManager
{
	internal static (Card Card, CardDestination OriginalDestination, int OriginalIndex)? CardBeingHackinglyPlayed;
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler)), priority: Priority.High)
		);
	}
	
	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("hand"),
					ILMatches.AnyLdloc,
					ILMatches.Ldfld("card"),
					ILMatches.Call("Contains")
				])
				.Find(ILMatches.Brtrue.GetBranchTarget(out var branchTarget))
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_RemoveFromHandIfHackinglyPlayed)))
				])
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("hand"),
					ILMatches.AnyLdloc,
					ILMatches.Ldfld("card"),
					ILMatches.Call("Remove"),
					ILMatches.Instruction(OpCodes.Pop)
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler_RemoveCardFromWhereverItIsIfNeeded)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void Combat_TryPlayCard_Transpiler_RemoveFromHandIfHackinglyPlayed(Combat combat, State state, Card card)
	{
		if (CardBeingHackinglyPlayed is not { } cardBeingHackinglyPlayed || cardBeingHackinglyPlayed.Card != card)
			return;
		combat.hand.Remove(card);

		var list = cardBeingHackinglyPlayed.OriginalDestination switch
		{
			CardDestination.Deck => state.deck,
			CardDestination.Hand => combat.hand,
			CardDestination.Discard => combat.discard,
			CardDestination.Exhaust => combat.exhausted,
			_ => []
		};
		list.Insert(Math.Min(cardBeingHackinglyPlayed.OriginalIndex, list.Count), card);
	}

	private static void Combat_TryPlayCard_Transpiler_RemoveCardFromWhereverItIsIfNeeded(State state, Card card)
	{
		if (card == CardBeingHackinglyPlayed?.Card)
			state.RemoveCardFromWhereverItIs(card.uuid);
	}
}

public sealed class APlaySpecificCardFromAnywhere : CardAction, IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction
{
	public required HashSet<int> CardIds { get; set; }
	public int Amount { get; set; } = 1;
	public bool ShowTheCardIfNotInHand { get; set; } = true;

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	[JsonProperty]
	private CardDestination? OriginalDestination;

	[JsonProperty]
	private int OriginalIndex;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		if (Amount <= 0 || CardIds.Count == 0)
		{
			timer = 0;
			return;
		}
		if (Amount != 1 || CardIds.Count != 1)
		{
			timer = 0;
			c.QueueImmediate(new APlayRandomCardsFromAnywhere { CardIds = CardIds, Amount = Amount, ShowTheCardIfNotInHand = ShowTheCardIfNotInHand });
			return;
		}

		var cardId = CardIds.Single();
		if (c.hand.Concat(s.deck).Concat(c.discard).Concat(c.exhausted).FirstOrDefault(c => c.uuid == cardId) is not { } card)
			return;

		var entry = GetEntry();

		if (c.hand.Contains(card))
		{
			PlayCardAndFixQueue(withHacks: OriginalDestination is not null);
			return;
		}

		if (!ShowTheCardIfNotInHand)
		{
			c.hand.Add(card);
			PlayCardAndFixQueue(withHacks: true);
			return;
		}

		s.RemoveCardFromWhereverItIs(cardId);

		var handCopy = c.hand.ToList();
		c.hand.Clear();
		c.SendCardToHand(s, card);
		c.hand.InsertRange(0, handCopy);

		c.QueueImmediate(new APlaySpecificCardFromAnywhere { CardIds = [card.uuid], OriginalDestination = entry?.OriginalDestination, OriginalIndex = entry?.OriginalIndex ?? 0 });
		c.QueueImmediate(new ADelay { time = -0.2 });

		(Card Card, CardDestination OriginalDestination, int OriginalIndex)? GetEntry()
		{
			if (this.OriginalDestination is { } originalDestination)
				return (card, originalDestination, this.OriginalIndex);

			int index;
			if ((index = s.deck.IndexOf(card)) != -1)
				return (card, CardDestination.Deck, index);
			if ((index = c.hand.IndexOf(card)) != -1)
				return (card, CardDestination.Hand, index);
			if ((index = c.discard.IndexOf(card)) != -1)
				return (card, CardDestination.Discard, index);
			if ((index = c.exhausted.IndexOf(card)) != -1)
				return (card, CardDestination.Exhaust, index);

			return null;
		}

		void PlayCardAndFixQueue(bool withHacks)
		{
			if (withHacks)
				PlaySpecificCardFromAnywhereManager.CardBeingHackinglyPlayed = entry;
			var queue = c.cardActions.Where(a => a != this).ToList();
			c.cardActions.Clear();
			c.TryPlayCard(s, card, playNoMatterWhatForFree: true);
			c.cardActions.AddRange(queue);
			if (withHacks)
				PlaySpecificCardFromAnywhereManager.CardBeingHackinglyPlayed = null;
		}
	}
	
	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetCardIds(HashSet<int> value)
	{
		CardIds = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetAmount(int value)
	{
		Amount = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetShowTheCardIfNotInHand(bool value)
	{
		ShowTheCardIfNotInHand = value;
		return this;
	}
}

public sealed class APlayRandomCardsFromAnywhere : CardAction, IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction
{
	public required HashSet<int> CardIds { get; set; }
	public required int Amount { get; set; }
	public bool ShowTheCardIfNotInHand { get; set; } = true;

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		var potentialCards = CardIds
			.Order()
			.Select(s.FindCard)
			.OfType<Card>()
			.Where(c => !c.GetDataWithOverrides(s).unplayable);

		foreach (var card in potentialCards.Shuffle(s.rngActions).Take(Amount))
			c.QueueImmediate(new APlaySpecificCardFromAnywhere { CardIds = [card.uuid], ShowTheCardIfNotInHand = ShowTheCardIfNotInHand });
	}
	
	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetCardIds(HashSet<int> value)
	{
		CardIds = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetAmount(int value)
	{
		Amount = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetShowTheCardIfNotInHand(bool value)
	{
		ShowTheCardIfNotInHand = value;
		return this;
	}
}