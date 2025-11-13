using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;
using Shockah.Shared;

namespace Shockah.DuoArtifacts;

internal static class EventsPatches
{
	private static ModEntry Instance => ModEntry.Instance;
	
	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Events), nameof(Events.BootSequence)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Events_BootSequence_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> Events_BootSequence_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<List<Choice>>(originalMethod).GetLocalIndex(out var choicesLocalIndex).ExtractLabels(out var labels),
					ILMatches.Ldloc<Rand>(originalMethod),
					ILMatches.Call("Shuffle"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldloc, choicesLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Events_BootSequence_Transpiler_ModifyChoices))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, Instance.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static void Events_BootSequence_Transpiler_ModifyChoices(State state, List<Choice> choices)
	{
		if (state.EnumerateAllArtifacts().Any(a => Instance.Database.IsDuoArtifact(a)))
			return;
		if (!Instance.Settings.ProfileBased.Current.BootSequenceUpsideEnabled)
			return;
		if (DuoArtifactOfferingAction.GetMatchingDuoArtifactTypes(state).Count == 0)
			return;
		
		choices.Add(new DuoUpsideChoice
		{
			label = I18n.DuoArtifactBootSequenceOption.Replace("{{Times}}", Instance.Settings.ProfileBased.Current.BootSequenceUpsideCards.ToString()),
			key = ".zone_first",
			actions = [new DuoArtifactOfferingAction()]
		});
	}

	private sealed class DuoArtifactOfferingAction : CardAction
	{
		public static List<Type> GetMatchingDuoArtifactTypes(State s)
			=> Instance.Database.GetMatchingDuoArtifactTypes(s.characters.Select(character => character.deckType).WhereNotNull()).ToList();
		
		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			timer = 0;

			var duoTypes = GetMatchingDuoArtifactTypes(s);
			if (duoTypes.Count == 0)
				return null;

			var duoType = duoTypes.Random(s.rngArtifactOfferings);
			if (Instance.Helper.Content.Artifacts.LookupByArtifactType(duoType) is not { } artifactEntry)
				return null;
			
			s.GetCurrentQueue().InsertRange(0, Enumerable.Range(0, Instance.Settings.ProfileBased.Current.BootSequenceUpsideCards).Select(_ => new DuoOwnerCardOfferingAction { ArtifactKey = artifactEntry.UniqueName }));
			return new ArtifactReward
			{
				artifacts = [(Artifact)Activator.CreateInstance(duoType)!],
				canSkip = false
			};
		}
	}

	private sealed class DuoOwnerCardOfferingAction : CardAction
	{
		public required string ArtifactKey;
		
		public override Route? BeginWithRoute(G g, State s, Combat c)
		{
			timer = 0;

			if (!DB.artifacts.TryGetValue(ArtifactKey, out var duoType))
				return null;
			if (Instance.Database.GetDuoArtifactTypeOwnership(duoType) is not { } owners)
				return null;

			var oldCharacters = s.characters.ToList();
			try
			{
				s.characters.RemoveAll(character => character.deckType is { } deck && !owners.Contains(deck));
				
				return new CardReward
				{
					cards = CardReward.GetOffering(s, 3, rarityOverride: Rarity.common, isEvent: true),
					canSkip = false,
				};
			}
			finally
			{
				s.characters.Clear();
				s.characters.AddRange(oldCharacters);
			}
		}
	}
}
