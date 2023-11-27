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

	private static void ArtifactReward_GetOffering_Postfix(State s, List<ArtifactPool>? limitPools, List<Artifact> __result, Rand? rngOverride)
	{
		if (limitPools is null || !limitPools.Contains(ArtifactPool.Boss))
			return;

		Rand random = rngOverride ?? s.rngArtifactOfferings;
		double duoArtifactChance = __result.Count * (1.0 / 3.0) / (s.artifacts.Count(a => a is DuoArtifact) + 1);
		if (!random.Chance(duoArtifactChance))
			return;

		var possibleDuoArtifacts = Instance.InstantiateDuoArtifacts(new Deck[] { Deck.dizzy, Deck.riggs, Deck.peri, Deck.goat, Deck.eunice, Deck.hacker, Deck.shard })
		//var possibleDuoArtifacts = Instance.InstantiateDuoArtifacts(s.characters.Select(c => c.deckType!.Value))
			.Where(duoArtifact => !s.artifacts.Any(artifact => artifact.Key() == duoArtifact.Key()))
			.ToList();
		if (possibleDuoArtifacts.Count == 0)
			return;

		int slotToReplace = random.NextInt() % __result.Count;
		int duoToTake = random.NextInt() % possibleDuoArtifacts.Count;
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

		var characters = Instance.GetCharactersForDuoArtifact(duoArtifact)?.OrderBy(c => c.Key()).ToList();
		if (characters is null)
			return color;

		var firstColor = DB.decks[characters[0]].color;
		var secondColor = DB.decks[characters[1]].color;
		return Color.Lerp(firstColor, secondColor, Math.Sin(Instance.TotalGameTime.TotalSeconds * 4) / 2 + 0.5);
	}
}
