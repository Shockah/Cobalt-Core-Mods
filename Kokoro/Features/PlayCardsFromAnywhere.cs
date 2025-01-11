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
using Newtonsoft.Json.Converters;

namespace Shockah.Kokoro;

partial class ApiImplementation
{
	#region V1
	
	partial class ActionApiImplementation
	{
		public CardAction MakePlaySpecificCardFromAnywhere(int cardId, bool showTheCardIfNotInHand = true)
			=> new APlaySpecificCardFromAnywhere { Cards = [(CardId: cardId, FallbackCard: null)], ShowTheCardIfNotInHand = showTheCardIfNotInHand };

		public CardAction MakePlayRandomCardsFromAnywhere(IEnumerable<int> cardIds, int amount = 1, bool showTheCardIfNotInHand = true)
			=> new APlayRandomCardsFromAnywhere { Amount = amount, ShowTheCardIfNotInHand = showTheCardIfNotInHand }.SetCardIds(cardIds).AsCardAction;
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
				=> new APlaySpecificCardFromAnywhere { Cards = [(CardId: cardId, FallbackCard: null)] };

			public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction MakeAction(Card card)
				=> new APlaySpecificCardFromAnywhere { Cards = [(CardId: card.uuid, FallbackCard: card)] };

			public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction MakeAction(IEnumerable<int> cardIds, int amount = 1)
				=> new APlayRandomCardsFromAnywhere { Amount = amount }.SetCardIds(cardIds);

			public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction MakeAction(IEnumerable<Card> cards, int amount = 1)
				=> new APlayRandomCardsFromAnywhere { Amount = amount }.SetCards(cards);

			public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction MakeAction(IEnumerable<(int CardId, Card? FallbackCard)> cards, int amount = 1)
				=> new APlayRandomCardsFromAnywhere { Amount = amount }.SetCards(cards);

			public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction? AsModifyAction(CardAction action)
				=> action as IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction;

			public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction MakeModifyAction(int cardId, CardAction action)
				=> new AModifyCardAnywhere { CardId = cardId, Action = action };
		}
	}
}

internal static class PlaySpecificCardFromAnywhereManager
{
	internal static (Card Card, CardDestination OriginalDestination, int OriginalIndex)? CardBeingHackinglyPlayed;
	internal static Card? CardBeingModified;
	
	internal static void Setup(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_TryPlayCard_Transpiler)), priority: Priority.High)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.RenderCards)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Combat_RenderCards_Postfix_Last)), priority: Priority.Last)
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

	private static void Combat_RenderCards_Postfix_Last(G g)
	{
		if (CardBeingModified is not { } card)
			return;

		var cardUiKey = card.UIKey();
		if (g.boxes.Any(b => b.key == cardUiKey))
			return;
		
		card.Render(g);
		CardBeingModified = null;
	}
}

public sealed class APlaySpecificCardFromAnywhere : CardAction, IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction
{
	public IList<(int CardId, Card? FallbackCard)> Cards { get; set; } = [];
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
		if (Amount <= 0 || Cards.Count == 0)
		{
			timer = 0;
			return;
		}
		if (Amount != 1 || Cards.Count != 1)
		{
			timer = 0;
			c.QueueImmediate(new APlayRandomCardsFromAnywhere { Cards = Cards, Amount = Amount, ShowTheCardIfNotInHand = ShowTheCardIfNotInHand });
			return;
		}

		var cardEntry = Cards.Single();
		if ((c.hand.Concat(s.deck).Concat(c.discard).Concat(c.exhausted).FirstOrDefault(c => c.uuid == cardEntry.CardId) ?? cardEntry.FallbackCard) is not { } card)
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

		s.RemoveCardFromWhereverItIs(card.uuid);

		var handCopy = c.hand.ToList();
		c.hand.Clear();
		c.SendCardToHand(s, card);
		c.hand.InsertRange(0, handCopy);

		c.QueueImmediate(new APlaySpecificCardFromAnywhere { Cards = [cardEntry], OriginalDestination = entry?.OriginalDestination, OriginalIndex = entry?.OriginalIndex ?? 0 });
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
	
	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetCardIds(IEnumerable<int> value)
	{
		Cards = value.Select<int, (int CardId, Card? FallbackCard)>(id => (CardId: id, FallbackCard: null)).ToList();
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetCards(IEnumerable<Card> value)
	{
		Cards = value.Select(c => (CardId: c.uuid, FallbackCard: (Card?)c)).ToList();
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetCards(IEnumerable<(int CardId, Card? FallbackCard)> value)
	{
		Cards = value.ToList();
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
	public IList<(int CardId, Card? FallbackCard)> Cards { get; set; } = [];
	public required int Amount { get; set; }
	public bool ShowTheCardIfNotInHand { get; set; } = true;

	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		var potentialCards = Cards
			.Select(e => s.FindCard(e.CardId) ?? e.FallbackCard)
			.OfType<Card>()
			.Where(c => !c.GetDataWithOverrides(s).unplayable);

		foreach (var card in potentialCards.Shuffle(s.rngActions).Take(Amount))
			c.QueueImmediate(new APlaySpecificCardFromAnywhere { Cards = [(CardId: card.uuid, FallbackCard: card)], ShowTheCardIfNotInHand = ShowTheCardIfNotInHand });
	}
	
	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetCardIds(IEnumerable<int> value)
	{
		Cards = value.Select<int, (int CardId, Card? FallbackCard)>(id => (CardId: id, FallbackCard: null)).ToList();
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetCards(IEnumerable<Card> value)
	{
		Cards = value.Select(c => (CardId: c.uuid, FallbackCard: (Card?)c)).ToList();
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IPlayCardsFromAnywhereAction SetCards(IEnumerable<(int CardId, Card? FallbackCard)> value)
	{
		Cards = value.ToList();
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

public sealed class AModifyCardAnywhere : CardAction, IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction
{
	[JsonConverter(typeof(StringEnumConverter))]
	private enum AnimationPhase
	{
		NotStarted,
		ShowCard,
		PrePause,
		Modification,
		PostPause,
		HideCard,
		Ended,
	}
	
	public required int CardId { get; set; }
	public required CardAction Action { get; set; }
	public bool MoveIfInHand { get; set; }
	public double PrePauseTime { get; set; } = TIMER_DEFAULT * 0.5;
	public double PostPauseTime { get; set; } = TIMER_DEFAULT * 1.5;
	public bool FlipOnBegin { get; set; } = true;
	public bool FlipOnEnd { get; set; }
	public bool FlopOnBegin { get; set; }
	public bool FlopOnEnd { get; set; }

	[JsonProperty]
	private AnimationPhase Phase = AnimationPhase.NotStarted;

	[JsonProperty]
	private Vec OriginalCardTargetPosition;

	[JsonProperty]
	private Vec LastCardPosition;

	[JsonProperty]
	private double OriginalCardDrawAnimation;

	[JsonProperty]
	private double LastCardDrawAnimation;

	[JsonProperty]
	private double WaitingTime;
	
	[JsonIgnore]
	public CardAction AsCardAction
		=> this;

	public override List<Tooltip> GetTooltips(State s)
		=> Action.GetTooltips(s);

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		Phase = AnimationPhase.NotStarted;
		
		if (s.FindCard(CardId) is not { } card)
		{
			timer = 0;
			return;
		}

		OriginalCardTargetPosition = card.targetPos;
		OriginalCardDrawAnimation = card.drawAnim;
		LastCardDrawAnimation = card.drawAnim;
		SwitchToNextPhase(g, s, c, card);
		PlaySpecificCardFromAnywhereManager.CardBeingModified = card;
	}

	public override void Update(G g, State s, Combat c)
	{
		if (Phase >= AnimationPhase.Ended || s.FindCard(CardId) is not { } card)
		{
			timer = 0;
			PlaySpecificCardFromAnywhereManager.CardBeingModified = null;
			return;
		}

		card.isForeground = true;
		card.drawAnim = Mutil.MoveTowards(LastCardDrawAnimation, Phase is >= AnimationPhase.ShowCard and < AnimationPhase.HideCard ? 1 : OriginalCardDrawAnimation, g.dt * 10.0);
		LastCardDrawAnimation = card.drawAnim;
		PlaySpecificCardFromAnywhereManager.CardBeingModified = card;
		
		switch (Phase)
		{
			case AnimationPhase.NotStarted:
				break;
			case AnimationPhase.ShowCard:
				HandleShowCardPhase(g, s, c, card);
				break;
			case AnimationPhase.PrePause:
				HandlePrePausePhase(g, s, c, card);
				break;
			case AnimationPhase.Modification:
				HandleModificationPhase(g, s, c, card);
				break;
			case AnimationPhase.PostPause:
				HandlePostPausePhase(g, s, c, card);
				break;
			case AnimationPhase.HideCard:
				HandleHideCardPhase(g, s, c, card);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void SwitchToNextPhase(G g, State s, Combat c, Card card)
	{
		if (Phase == AnimationPhase.Ended)
			return;
		Phase++;
		timer = 1;
		LastCardPosition = card.pos;

		switch (Phase)
		{
			case AnimationPhase.NotStarted:
				break;
			case AnimationPhase.ShowCard:
				if (!MoveIfInHand && c.hand.Contains(card))
					SwitchToNextPhase(g, s, c, card);
				else
					card.waitBeforeMoving = 0;
				
				break;
			case AnimationPhase.PrePause:
				if ((!MoveIfInHand && c.hand.Contains(card)) || PrePauseTime <= 0)
					SwitchToNextPhase(g, s, c, card);
				else
					WaitingTime = PrePauseTime;
				
				break;
			case AnimationPhase.Modification:
				Action.selectedCard = card;
				Action.Begin(g, s, c);
				
				if (FlipOnBegin)
					card.flipAnim = 1;
				if (FlopOnBegin)
					card.flopAnim = 1;
				
				break;
			case AnimationPhase.PostPause:
				if ((!MoveIfInHand && c.hand.Contains(card)) || (PostPauseTime <= 0 && !FlipOnEnd && !FlopOnEnd))
				{
					SwitchToNextPhase(g, s, c, card);
				}
				else
				{
					WaitingTime = PostPauseTime;
					if (FlipOnEnd)
						card.flipAnim = 1;
					if (FlopOnEnd)
						card.flopAnim = 1;
				}
				
				break;
			case AnimationPhase.HideCard:
				if (!MoveIfInHand && c.hand.Contains(card))
				{
					SwitchToNextPhase(g, s, c, card);
				}
				else
				{
					card.drawAnim = 1;
					card.waitBeforeMoving = 0;
				}
				
				break;
			case AnimationPhase.Ended:
				timer = 0;
				PlaySpecificCardFromAnywhereManager.CardBeingModified = null;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void HandleShowCardPhase(G g, State s, Combat c, Card card)
	{
		card.pos = LastCardPosition;
		
		UpdateCardPosition(g, card);
		if (Math.Abs((card.targetPos - card.pos).len()) <= 1)
			SwitchToNextPhase(g, s, c, card);
	}

	private void HandlePrePausePhase(G g, State s, Combat c, Card card)
	{
		card.pos = LastCardPosition;
		card.drawAnim = 1;
		card.waitBeforeMoving = card.flipAnim > 0 || card.flopAnim > 0 ? 0 : 1;
		LastCardDrawAnimation = 1;
		
		WaitingTime -= g.dt;
		if (WaitingTime <= 0)
			SwitchToNextPhase(g, s, c, card);
	}

	private void HandleModificationPhase(G g, State s, Combat c, Card card)
	{
		card.pos = LastCardPosition;
		card.drawAnim = 1;
		card.waitBeforeMoving = card.flipAnim > 0 || card.flopAnim > 0 ? 0 : 1;
		LastCardDrawAnimation = 1;

		if (Action.timer > 0)
		{
			Action.selectedCard = card;
			Action.Update(g, s, c);
		}

		if (card.flipAnim != 0 || card.flopAnim != 0)
			return;
		
		SwitchToNextPhase(g, s, c, card);
	}

	private void HandlePostPausePhase(G g, State s, Combat c, Card card)
	{
		card.drawAnim = 1;
		card.waitBeforeMoving = card.flipAnim > 0 || card.flopAnim > 0 ? 0 : 1;
		LastCardDrawAnimation = 1;
		
		if (card.flipAnim != 0 || card.flopAnim != 0)
		{
			card.pos = LastCardPosition;
			return;
		}
		
		WaitingTime -= g.dt;
		if (WaitingTime <= 0)
			SwitchToNextPhase(g, s, c, card);
	}

	private void HandleHideCardPhase(G g, State s, Combat c, Card card)
	{
		UpdateCardPosition(g, card, OriginalCardTargetPosition);
		
		if (Math.Abs((card.targetPos - card.pos).len()) <= 1)
			SwitchToNextPhase(g, s, c, card);
	}

	private void UpdateCardPosition(G g, Card card, Vec? targetPos = null)
	{
		card.targetPos = targetPos ?? new(Combat.cardCenter.x - 30, Combat.cardCenter.y - 120);
		card.pos = Mutil.LerpDeltaSnap(LastCardPosition, card.targetPos, 12.0, g.dt);
		LastCardPosition = card.pos;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction SetCardId(int value)
	{
		CardId = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction SetAction(CardAction value)
	{
		Action = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction SetMoveIfInHand(bool value)
	{
		MoveIfInHand = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction SetPrePauseTime(double value)
	{
		PrePauseTime = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction SetPostPauseTime(double value)
	{
		PostPauseTime = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction SetFlipOnBegin(bool value)
	{
		FlipOnBegin = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction SetFlipOnEnd(bool value)
	{
		FlipOnEnd = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction SetFlopOnBegin(bool value)
	{
		FlopOnBegin = value;
		return this;
	}

	public IKokoroApi.IV2.IPlayCardsFromAnywhereApi.IModifyCardAnywhereAction SetFlopOnEnd(bool value)
	{
		FlopOnEnd = value;
		return this;
	}
}