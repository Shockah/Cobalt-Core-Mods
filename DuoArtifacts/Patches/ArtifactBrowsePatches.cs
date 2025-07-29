using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Input;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.DuoArtifacts;

internal static class ArtifactBrowsePatches
{
	private static ModEntry Instance => ModEntry.Instance;
	
	private static Deck? LastDeck;

	public static void Apply(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactBrowse), nameof(ArtifactBrowse.GetSections)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_GetSections_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_Render_Prefix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_Render_Transpiler))
		);
	}
	
	private static void ArtifactBrowse_GetSections_Postfix(State state, ref IEnumerable<ArtifactBrowse.Section> __result)
	{
		var sections = __result.ToList();
		var allDuos = Instance.Database.InstantiateAllDuoArtifacts().ToList();

		foreach (var section in sections)
		{
			if (section.artifacts.Count == 0)
				continue;

			var deck = section.artifacts[0].GetMeta().owner;
			if (deck == Deck.colorless)
				continue;
			
			var playableDeck = deck == Deck.catartifact ? Deck.colorless : deck;
			if (!NewRunOptions.allChars.Contains(playableDeck))
				continue;
			if (Character.GetDisplayName(deck, state) != section.title())
				continue;
			
			LastDeck = deck;
			
			section.artifacts.AddRange(
				allDuos
					.Select(duo => (Duo: duo, Owners: Instance.Database.GetDuoArtifactOwnership(duo)))
					.Where(e => e.Owners?.Contains(playableDeck) ?? false)
					.Select(e => (Duo: e.Duo, SecondaryOwners: e.Owners!.Where(owner => owner != playableDeck).OrderBy(owner => NewRunOptions.allChars.IndexOf(owner)).ToList()))
					.OrderBy(e => e.SecondaryOwners, new DuoByOwnerComparer())
					.Select(e => e.Duo)
			);
		}

		__result = sections;
	}

	private static void Artifact_Render_Prefix(Artifact __instance, G g, Vec restingPosition)
	{
		if (FeatureFlags.GifMode)
			return;
		if (FeatureFlags.Debug && Input.GetKeyHeld(Keys.F1))
			return;
		if ((g.metaRoute?.subRoute as Codex)?.subRoute is not ArtifactBrowse)
			return;
		if (Instance.Database.GetDuoArtifactOwnership(__instance) is not { } owners)
			return;

		var parentBox = g.uiStack.Peek();
		var baseX = parentBox.rect.x + restingPosition.x - 6;
		var baseY = parentBox.rect.y + restingPosition.y - 6;

		var ownersList = owners
			.OrderBy(d => !(LastDeck == d || (LastDeck == Deck.catartifact && d == Deck.colorless)))
			.ThenBy(NewRunOptions.allChars.IndexOf)
			.Select(c => c == Deck.catartifact ? Deck.colorless : c)
			.ToList();

		if (ownersList.Count == 3)
			for (var i = 0; i < ownersList.Count; i++)
				Draw.Sprite((Spr)Instance.TrioGlowSprites[i].Id!.Value, baseX, baseY, color: DB.decks[ownersList[i]].color.fadeAlpha(0.4));
		else
			for (var i = 0; i < 2; i++)
				Draw.Sprite(
					(Spr)Instance.DuoGlowSprites[i].Id!.Value, baseX, baseY,
					color: (i == 0 ? DB.decks[ownersList[0]].color : Instance.Database.GetDynamicColorForArtifact(__instance, ownersList[0])).fadeAlpha(0.4)
				);
	}

	private static IEnumerable<CodeInstruction> Artifact_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call(AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.UIKey))))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArtifactBrowsePatches), nameof(Artifact_Render_Transpiler_ModifyUIKey)))
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

	private static UIKey Artifact_Render_Transpiler_ModifyUIKey(UIKey baseKey, Artifact artifact, G g, Vec restingPosition)
	{
		if (FeatureFlags.GifMode)
			return baseKey;
		if ((g.metaRoute?.subRoute as Codex)?.subRoute is not ArtifactBrowse route)
			return baseKey;
		if (Instance.Database.GetDuoArtifactOwnership(artifact) is null)
			return baseKey;

		var parentBox = g.uiStack.Peek();
		var baseX = parentBox.rect.x + restingPosition.x;
		var baseY = parentBox.rect.y + restingPosition.y - route.scroll;

		var newKey = new UIKey(baseKey.k, baseKey.v, $"{baseKey.str}__{(int)baseX}__{(int)baseY}");

		if (route.artifactToScrollYCache.Remove(baseKey, out var scrollYCache))
			route.artifactToScrollYCache[newKey] = scrollYCache;

		return newKey;
	}

	private sealed class DuoByOwnerComparer : IComparer<List<Deck>>
	{
		public int Compare(List<Deck>? x, List<Deck>? y)
		{
			if (x is null && y is null)
				return 0;
			if (x is null)
				return -1;
			if (y is null)
				return 1;

			if (x.Count != y.Count)
				return x.Count < y.Count ? -1 : 1;

			for (var i = 0; i < x.Count; i++)
			{
				var xIndex = NewRunOptions.allChars.IndexOf(x[i]);
				var yIndex = NewRunOptions.allChars.IndexOf(y[i]);

				if (xIndex == -1 && yIndex == -1)
					return 0;
				if (xIndex == -1)
					return 1;
				if (yIndex == -1)
					return -1;
				if (xIndex != yIndex)
					return xIndex < yIndex ? -1 : 1;
			}

			return 0;
		}
	}
}
