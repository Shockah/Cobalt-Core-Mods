using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel;

namespace Shockah.NewLight;

internal sealed class GhostArtifact : Artifact, IRegisterable
{
	private const double OWNED_REPLACEMENT_CHANCE = 0.5;
	private const double UNOWNED_REPLACEMENT_CHANCE = 0.3333;

	public static IArtifactEntry Entry { get; private set; } = null!;
	
	public static void Register(IPluginPackage<IModManifest> package, IModHelper helper)
	{
		Entry = helper.Content.Artifacts.RegisterArtifact("Ghost", new()
		{
			ArtifactType = MethodBase.GetCurrentMethod()!.DeclaringType!,
			Meta = new()
			{
				owner = ModEntry.Instance.GuardianDeck.Deck,
				pools = [ArtifactPool.EventOnly],
				unremovable = true,
			},
			Sprite = helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/Artifacts/Special/Ghost.png")).Sprite,
			Name = ModEntry.Instance.AnyLocalizations.Bind(["Artifact", "Special", "Ghost", "Name"]).Localize,
			Description = ModEntry.Instance.AnyLocalizations.Bind(["Artifact", "Special", "Ghost", "Description"]).Localize
		});
		
		ModEntry.Instance.Harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardReward), nameof(CardReward.GetOffering)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Transpiler))
		);
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> CardReward_GetOffering_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld(nameof(State.rngCardOfferingsMidcombat)),
					ILMatches.Stloc<Rand>(originalMethod).GetLocalIndex(out var rngSourceLocalIndex),
				])
				.Find([
					ILMatches.AnyLdloc,
					ILMatches.Ldarg(2),
					ILMatches.Stloc<Deck?>(originalMethod),
					ILMatches.Ldloca<Deck?>(originalMethod),
					ILMatches.Call("get_HasValue"),
				])
				.Find(ILMatches.Stfld("deck").ExtractLabels(out var labels))
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Ldloc, rngSourceLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Transpiler_ModifyDeck))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static Deck? CardReward_GetOffering_Transpiler_ModifyDeck(Deck? deck, State state, Deck? limitDeck, Rand rand)
	{
		if (!state.TryGetGhostArtifact(out _, out var ownerDeck))
			return deck;
		if (ownerDeck is not null && deck != ownerDeck)
			return deck;
		
		var chance = ownerDeck is null ? UNOWNED_REPLACEMENT_CHANCE : OWNED_REPLACEMENT_CHANCE;
		if (rand.Next() > chance)
			return deck;
		
		return ModEntry.Instance.GuardianDeck.Deck;
	}
}

internal static class GhostArtifactExt
{
	extension(State state)
	{
		public bool TryGetGhostArtifact([MaybeNullWhen(false)] out GhostArtifact ghostArtifact, out Deck? ownerDeck)
		{
			foreach (var artifact in state.artifacts)
			{
				if (artifact is not GhostArtifact ghost)
					continue;
				ghostArtifact = ghost;
				ownerDeck = null;
				return true;
			}

			foreach (var character in state.characters)
			{
				foreach (var artifact in character.artifacts)
				{
					if (artifact is not GhostArtifact ghost)
						continue;
					ghostArtifact = ghost;
					ownerDeck = character.deckType;
					return true;
				}
			}

			ghostArtifact = null;
			ownerDeck = null;
			return false;
		}
	}
}