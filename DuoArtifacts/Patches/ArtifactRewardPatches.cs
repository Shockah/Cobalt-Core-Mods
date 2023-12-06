using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace Shockah.DuoArtifacts;

internal static class ArtifactRewardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	private const int ArtifactsRequiredForEligibility = 1;
	private const int RareCardsRequiredForEligibility = 1;
	private const int AnyCardsRequiredForEligibility = 5;

	private const double SingleColorTransitionAnimationLengthSeconds = 1;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.GetOffering)),
			postfix: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_GetOffering_Postfix))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.Render)),
			transpiler: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_Render_Transpiler))
		);
	}

	private static IEnumerable<Deck> GetCharactersEligibleForDuoArtifacts(State state)
	{
		bool IsEligible(Character character)
		{
			if (character.artifacts.Count >= ArtifactsRequiredForEligibility)
				return true;

			var characterCardsInDeck = state.deck
				.Where(c => DB.cardMetas.TryGetValue(c.Key(), out var meta) && !meta.dontOffer && meta.deck == character.deckType)
				.ToList();
			if (characterCardsInDeck.Count >= AnyCardsRequiredForEligibility)
				return true;

			var rareCharacterCardsInDeck = characterCardsInDeck
				.Where(c => DB.cardMetas.TryGetValue(c.Key(), out var meta) && (int)meta.rarity >= (int)Rarity.rare)
				.ToList();
			if (rareCharacterCardsInDeck.Count >= RareCardsRequiredForEligibility)
				return true;

			return false;
		}

		//return NewRunOptions.allChars;

		foreach (var character in state.characters)
			if (character.deckType is { } deck && IsEligible(character))
				yield return deck == Deck.colorless ? Deck.catartifact : deck;
	}

	private static void ArtifactReward_GetOffering_Postfix(State s, List<ArtifactPool>? limitPools, List<Artifact> __result, Rand? rngOverride)
	{
		if (limitPools is null || limitPools.Contains(ArtifactPool.Boss))
			return;

		Rand random = rngOverride ?? s.rngArtifactOfferings;
		double duoArtifactChance = __result.Count * (1.0 / 3.0) / (s.EnumerateAllArtifacts().Count(a => a is DuoArtifact) + 1);
		if (!random.Chance(duoArtifactChance))
			return;

		var possibleDuoArtifacts = Instance.Database.InstantiateMatchingDuoArtifacts(GetCharactersEligibleForDuoArtifacts(s))
			.Where(duoArtifact => !s.EnumerateAllArtifacts().Any(a => Instance.Database.IsDuoArtifact(a) && Instance.Database.GetDuoArtifactOwnership(a)!.SetEquals(Instance.Database.GetDuoArtifactOwnership(duoArtifact)!)))
			.ToList();
		if (possibleDuoArtifacts.Count == 0)
			return;

		int duoToTake = random.NextInt() % possibleDuoArtifacts.Count;
		var duoArtifact = possibleDuoArtifacts[duoToTake];

		if (__result.Count <= 3)
		{
			__result.Add(duoArtifact);
			return;
		}

		IEnumerable<int> GetPossibleSlotsToReplace()
		{
			bool yieldedAny = false;

			// try any colorless artifact
			for (int i = 0; i < __result.Count; i++)
			{
				if (!DB.artifactMetas.TryGetValue(__result[i].Key(), out var meta))
					continue;
				if (meta.owner != Deck.colorless)
					continue;

				yieldedAny = true;
				yield return i;
			}

			if (yieldedAny)
				yield break;

			// no colorless artifacts; try any char-specific artifact
			for (int i = 0; i < __result.Count; i++)
			{
				if (!DB.artifactMetas.TryGetValue(__result[i].Key(), out var meta))
					continue;
				if (!NewRunOptions.allChars.Contains(meta.owner))
					continue;

				yieldedAny = true;
				yield return i;
			}

			if (yieldedAny)
				yield break;

			// no char-specific artifacts; did another mod replace rewards? nothing to return
		}

		var possibleSlots = GetPossibleSlotsToReplace().ToList();
		if (possibleSlots.Count == 0)
			return;

		int slotToReplace = possibleSlots[random.NextInt() % possibleSlots.Count];
		__result[slotToReplace] = possibleDuoArtifacts[duoToTake];
	}

	private static IEnumerable<CodeInstruction> ArtifactReward_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldfld("artifacts"),
					ILMatches.AnyLdloc,
					ILMatches.Call("get_Item"),
					ILMatches.Stloc<Artifact>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.CreateLdlocInstruction(out var ldlocArtifact)

				.ForEach(
					SequenceMatcherRelativeBounds.WholeSequence,
					new IElementMatch<CodeInstruction>[]
					{
						ILMatches.Ldfld(AccessTools.DeclaredField(typeof(DeckDef), nameof(DeckDef.color)))
					},
					matcher =>
					{
						return matcher
							.Insert(
								SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
								ldlocArtifact,
								new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_Render_Transpiler_ModifyDeckColor)))
							);
					},
					minExpectedOccurences: 2,
					maxExpectedOccurences: 2
				)

				.Find(ILMatches.Instruction(OpCodes.Ldflda, AccessTools.DeclaredField(typeof(DeckDef), nameof(DeckDef.color))))
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static Color ArtifactReward_Render_Transpiler_ModifyDeckColor(Color color, Artifact artifact)
	{
		if (artifact is not DuoArtifact duoArtifact)
			return color;

		var colors = Instance.Database.GetDuoArtifactOwnership(duoArtifact)
			?.OrderBy(c => c.Key())
			.Select(key => DB.decks[key].color)
			.ToList();
		if (colors is null)
			return color;

		static (Color, Color, double) GetLerpInfo(List<Color> colors, double totalFraction)
		{
			double singleFraction = 1.0 / colors.Count;
			int whichFraction = ((int)Math.Round(totalFraction / singleFraction) + colors.Count - 1) % colors.Count;
			double fractionStart = singleFraction * whichFraction;
			double fractionEnd = singleFraction * (whichFraction + 1);
			double fraction = (totalFraction - fractionStart) / (fractionEnd - fractionStart);
			return (colors[whichFraction], colors[(whichFraction + 1) % colors.Count], fraction);
		}

		double animationLength = colors.Count * SingleColorTransitionAnimationLengthSeconds;
		double animationPosition = Instance.TotalGameTime.TotalSeconds % animationLength;
		double totalFraction = animationPosition / animationLength;
		var (fromColor, toColor, fraction) = GetLerpInfo(colors, totalFraction);
		double lerpFraction = Math.Sin(fraction * Math.PI) * 0.5 + 0.5;
		return Color.Lerp(fromColor, toColor, lerpFraction);
	}
}
