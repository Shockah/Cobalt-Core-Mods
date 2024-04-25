using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Input;
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
	private static readonly Lazy<Func<ArtifactBrowse, Dictionary<UIKey, double>>> ArtifactToScrollYCacheGetter = new(() => AccessTools.DeclaredField(typeof(ArtifactBrowse), "artifactToScrollYCache").EmitInstanceGetter<ArtifactBrowse, Dictionary<UIKey, double>>());

	private static Deck? LastDeck;

	public static void Apply(Harmony harmony)
	{
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(ArtifactBrowse), nameof(ArtifactBrowse.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Transpiler))
		);
		harmony.TryPatch(
			logger: Instance.Logger!,
			original: () => AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_Render_Prefix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_Render_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> ArtifactBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Stloc<List<(Deck, List<KeyValuePair<string, Type>>)>>(originalMethod))
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArtifactBrowsePatches), nameof(ArtifactBrowse_Render_Transpiler_ModifyArtifacts)))
				)

				.Find(ILMatches.Stloc<(Deck, List<KeyValuePair<string, Type>>)>(originalMethod))
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArtifactBrowsePatches), nameof(ArtifactBrowse_Render_Transpiler_MarkLastArtifactSection)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static List<(Deck, List<KeyValuePair<string, Type>>)> ArtifactBrowse_Render_Transpiler_ModifyArtifacts(List<(Deck, List<KeyValuePair<string, Type>>)> allArtifacts)
	{
		var allDuos = Instance.Database.InstantiateAllDuoArtifacts().ToList();

		foreach (var (deck, artifacts) in allArtifacts)
		{
			if (deck == Deck.colorless)
				continue;
			if (deck != Deck.catartifact && !NewRunOptions.allChars.Contains(deck))
				continue;

			artifacts.AddRange(
				allDuos
					.Where(duo => Instance.Database.GetDuoArtifactOwnership(duo)?.Contains(deck == Deck.catartifact ? Deck.colorless : deck) ?? false)
					.Select(duo => new KeyValuePair<string, Type>(duo.Key(), duo.GetType()))
			);
		}

		return allArtifacts;
	}

	private static (Deck, List<KeyValuePair<string, Type>>) ArtifactBrowse_Render_Transpiler_MarkLastArtifactSection((Deck, List<KeyValuePair<string, Type>>) element)
	{
		LastDeck = element.Item1;
		return element;
	}

	private static void Artifact_Render_Prefix(Artifact __instance, G g, Vec restingPosition)
	{
		if (FeatureFlags.GifMode)
			return;
		if (FeatureFlags.Debug && Input.GetKeyHeld(Keys.F1))
			return;
		if ((g.metaRoute?.subRoute as Codex)?.subRoute is not ArtifactBrowse route)
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
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call(AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.UIKey))))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArtifactBrowsePatches), nameof(Artifact_Render_Transpiler_ModifyUIKey)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger!.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Name, ex);
			return instructions;
		}
	}

	private static UIKey Artifact_Render_Transpiler_ModifyUIKey(UIKey baseKey, Artifact artifact, G g, Vec restingPosition)
	{
		if (FeatureFlags.GifMode)
			return baseKey;
		if ((g.metaRoute?.subRoute as Codex)?.subRoute is not ArtifactBrowse route)
			return baseKey;
		if (Instance.Database.GetDuoArtifactOwnership(artifact) is not { } owners)
			return baseKey;

		var parentBox = g.uiStack.Peek();
		var baseX = parentBox.rect.x + restingPosition.x;
		var baseY = parentBox.rect.y + restingPosition.y;

		var newKey = new UIKey(baseKey.k, baseKey.v, $"{baseKey.str}__{(int)baseX}__{(int)baseY}");

		var artifactToScrollYCache = ArtifactToScrollYCacheGetter.Value(route);
		if (artifactToScrollYCache.TryGetValue(baseKey, out var scrollYCache))
		{
			artifactToScrollYCache.Remove(baseKey);
			artifactToScrollYCache[newKey] = scrollYCache;
		}

		return newKey;
	}
}
