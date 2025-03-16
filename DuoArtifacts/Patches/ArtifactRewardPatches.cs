using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.DuoArtifacts;

internal static class ArtifactRewardPatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.GetOffering)),
			prefix: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_GetOffering_Prefix)),
			postfix: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_GetOffering_Postfix)),
			finalizer: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_GetOffering_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.Render)),
			transpiler: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_Render_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.OnMouseDown)),
			postfix: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_OnMouseDown_Postfix))
		);
	}

	private static IEnumerable<Deck> GetCharactersEligibleForDuoArtifacts(State state)
	{
		foreach (var character in state.characters)
			if (character.deckType is { } deck && Instance.GetDuoArtifactEligibity(deck, state) == DuoArtifactEligibity.Eligible)
				yield return deck == Deck.catartifact ? Deck.colorless : deck;
	}

	private static void ArtifactReward_GetOffering_Prefix(State s)
		=> Instance.Database.FixArtifactMeta(Instance.Settings.ProfileBased.Current.OfferingMode, s, Instance.Helper.ModData.ObtainModData<HashSet<string>>(s, "DuosSeenThisRun"));

	private static void ArtifactReward_GetOffering_Postfix(State s, List<ArtifactPool>? limitPools, List<Artifact> __result, Rand? rngOverride)
	{
		if (limitPools is not null && limitPools.Contains(ArtifactPool.Boss))
			return;
		if (Instance.Settings.ProfileBased.Current.OfferingMode == ProfileSettings.OfferingModeEnum.Common)
			return;
		if (s.storyVars.oncePerRunTags.Contains(DuoArtifactDatabase.GetStoryVarsTagForMap(s.map)))
			return;

		var random = rngOverride ?? s.rngArtifactOfferings;
		var duosSeenThisRun = Instance.Helper.ModData.ObtainModData<HashSet<string>>(s, "DuosSeenThisRun");
		var possibleDuoArtifacts = Instance.Database.InstantiateMatchingDuoArtifacts(GetCharactersEligibleForDuoArtifacts(s))
			.Where(duoArtifact => !s.EnumerateAllArtifacts().Any(a => Instance.Database.IsDuoArtifact(a) && Instance.Database.GetDuoArtifactOwnership(a)!.SetEquals(Instance.Database.GetDuoArtifactOwnership(duoArtifact)!)))
			.Where(duoArtifact => Instance.Settings.ProfileBased.Current.OfferingMode switch
			{
				ProfileSettings.OfferingModeEnum.Common => false,
				ProfileSettings.OfferingModeEnum.Extra => true,
				ProfileSettings.OfferingModeEnum.ExtraOnceThenCommon => !duosSeenThisRun.Contains(duoArtifact.Key()),
				_ => throw new ArgumentOutOfRangeException()
			})
			.ToList();
		if (possibleDuoArtifacts.Count == 0)
			return;

		var duoToTake = random.NextInt() % possibleDuoArtifacts.Count;
		var duoArtifact = possibleDuoArtifacts[duoToTake];

		if (__result.Count <= 3)
		{
			__result.Add(duoArtifact);
			duosSeenThisRun.Add(duoArtifact.Key());
			return;
		}

		var possibleSlots = GetPossibleSlotsToReplace().ToList();
		if (possibleSlots.Count == 0)
			return;

		var slotToReplace = possibleSlots[random.NextInt() % possibleSlots.Count];
		__result[slotToReplace] = possibleDuoArtifacts[duoToTake];
		duosSeenThisRun.Add(duoArtifact.Key());

		IEnumerable<int> GetPossibleSlotsToReplace()
		{
			var yieldedAny = false;

			// try any colorless artifact
			for (var i = 0; i < __result.Count; i++)
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
			for (var i = 0; i < __result.Count; i++)
			{
				if (!DB.artifactMetas.TryGetValue(__result[i].Key(), out var meta))
					continue;
				if (!NewRunOptions.allChars.Contains(meta.owner))
					continue;

				// yieldedAny = true;
				yield return i;
			}

			// if (yieldedAny)
			// 	yield break;

			// no char-specific artifacts; did another mod replace rewards? nothing to return
		}
	}

	private static void ArtifactReward_GetOffering_Finalizer()
		=> Instance.Database.FixArtifactMeta(ProfileSettings.OfferingModeEnum.Extra, null, null);

	private static IEnumerable<CodeInstruction> ArtifactReward_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldfld("artifacts"),
					ILMatches.AnyLdloc,
					ILMatches.Call("get_Item"),
					ILMatches.Stloc<Artifact>(originalMethod).CreateLdlocInstruction(out var ldlocArtifact)
				])
				.ForEach(
					SequenceMatcherRelativeBounds.WholeSequence,
					[ILMatches.Ldfld(AccessTools.DeclaredField(typeof(DeckDef), nameof(DeckDef.color)))],
					matcher => matcher
						.Insert(
							SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
							ldlocArtifact,
							new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArtifactRewardPatches), nameof(ArtifactReward_Render_Transpiler_ModifyDeckColor)))
						),
					minExpectedOccurences: 2,
					maxExpectedOccurences: 2
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static Color ArtifactReward_Render_Transpiler_ModifyDeckColor(Color _, Artifact artifact)
		=> Instance.Database.GetDynamicColorForArtifact(artifact);

	private static void ArtifactReward_OnMouseDown_Postfix(ArtifactReward __instance, G g, Box b)
	{
		var index = b.key?.ValueFor(StableUK.artifactReward_artifact);
		if (index is null)
			return;

		var artifact = __instance.artifacts.ElementAtOrDefault(index.Value);
		if (artifact is null || !Instance.Database.IsDuoArtifact(artifact))
			return;

		g.state.storyVars.oncePerRunTags.Add(DuoArtifactDatabase.GetStoryVarsTagForMap(g.state.map));
	}
}
