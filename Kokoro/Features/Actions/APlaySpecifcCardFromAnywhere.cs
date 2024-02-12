using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Kokoro;

public sealed class APlaySpecificCardFromAnywhere : CardAction
{
	private static ModEntry Instance => ModEntry.Instance;

	private static (Card Card, CardDestination OriginalDestination, int OriginalIndex)? CardBeingHackinglyPlayed;

	public int CardId;
	public bool ShowTheCardIfNotInHand = true;

	[JsonProperty]
	private CardDestination? OriginalDestination = null;

	[JsonProperty]
	private int OriginalIndex = 0;

	internal static void ApplyPatches(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.TryPlayCard)),
			transpiler: new HarmonyMethod(typeof(APlaySpecificCardFromAnywhere), nameof(Combat_TryPlayCard_Transpiler))
		);
	}

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		Card? card = c.hand.Concat(s.deck).Concat(c.discard).Concat(c.exhausted).FirstOrDefault(c => c.uuid == CardId);
		if (card is null)
			return;

		(Card Card, CardDestination OriginalDestination, int OriginalIndex)? GetEntry()
		{
			if (card is null)
				return null;
			if (OriginalDestination is { } originalDestination)
				return (card, originalDestination, OriginalIndex);

			int index = -1;
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

		var entry = GetEntry();

		void PlayCardAndFixQueue(bool withHacks)
		{
			if (card is null)
				return;

			if (withHacks)
				CardBeingHackinglyPlayed = entry;
			var queue = c.cardActions.Where(a => a != this).ToList();
			c.cardActions.Clear();
			c.TryPlayCard(s, card, playNoMatterWhatForFree: true);
			c.cardActions.AddRange(queue);
			if (withHacks)
				CardBeingHackinglyPlayed = null;
		}

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

		s.RemoveCardFromWhereverItIs(CardId);

		var handCopy = c.hand.ToList();
		c.hand.Clear();
		c.SendCardToHand(s, card);
		c.hand.InsertRange(0, handCopy);

		c.QueueImmediate(new APlaySpecificCardFromAnywhere { CardId = card.uuid, OriginalDestination = entry?.OriginalDestination, OriginalIndex = entry?.OriginalIndex ?? 0 });
		c.QueueImmediate(new ADelay() { time = -0.2 });
	}

	private static IEnumerable<CodeInstruction> Combat_TryPlayCard_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("hand"),
					ILMatches.AnyLdloc,
					ILMatches.Ldfld("card"),
					ILMatches.Call("Contains")
				)
				.Find(ILMatches.Brtrue.GetBranchTarget(out var branchTarget))
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(APlaySpecificCardFromAnywhere), nameof(Combat_TryPlayCard_Transpiler_RemoveFromHandIfHackinglyPlayed)))
				)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("hand"),
					ILMatches.AnyLdloc,
					ILMatches.Ldfld("card"),
					ILMatches.Call("Remove"),
					ILMatches.Instruction(OpCodes.Pop)
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(APlaySpecificCardFromAnywhere), nameof(Combat_TryPlayCard_Transpiler_RemoveCardFromWhereverItIsIfNeeded)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
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
