using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.DuoArtifacts;

internal static class ArtifactBrowsePatches
{
	private static ModEntry Instance => ModEntry.Instance;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactBrowse), nameof(ArtifactBrowse.Render)),
			transpiler: new HarmonyMethod(typeof(ArtifactBrowsePatches), nameof(ArtifactBrowse_Render_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> ArtifactBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.LdcI4((int)Deck.colorless),
					ILMatches.Call("Contains"),
					ILMatches.Brfalse.GetBranchTarget(out var branchTarget)
				)
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldloc_2).WithLabels(labels),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArtifactBrowsePatches), nameof(ArtifactBrowse_Render_Transpiler_ModifyDecks)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static void ArtifactBrowse_Render_Transpiler_ModifyDecks(List<Deck> decks)
	{
		if (Instance.Database.GetAllDuoArtifactTypes().Any(t => Instance.Database.GetDuoArtifactTypeOwnership(t)?.Count == 2))
			decks.Add((Deck)Instance.Database.DuoArtifactDeck.Id!.Value);
		if (Instance.Database.GetAllDuoArtifactTypes().Any(t => Instance.Database.GetDuoArtifactTypeOwnership(t)?.Count == 3))
			decks.Add((Deck)Instance.Database.TrioArtifactDeck.Id!.Value);
		if (Instance.Database.GetAllDuoArtifactTypes().Any(t => Instance.Database.GetDuoArtifactTypeOwnership(t)?.Count >= 4))
			decks.Add((Deck)Instance.Database.ComboArtifactDeck.Id!.Value);
	}
}
