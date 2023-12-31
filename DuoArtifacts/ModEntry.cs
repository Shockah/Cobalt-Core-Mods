﻿using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.DuoArtifacts;

public sealed class ModEntry : IModManifest, IPrelaunchManifest, IApiProviderManifest, ISpriteManifest, IStatusManifest, IDeckManifest, IArtifactManifest, ICardManifest
{
	private const int ArtifactsRequiredForEligibility = 1;
	private const int RareCardsRequiredForEligibility = 1;
	private const int AnyCardsRequiredForEligibility = 5;

	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[] { new DependencyEntry<IModManifest>("Shockah.Kokoro", ignoreIfMissing: false) };

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal IKokoroApi KokoroApi { get; private set; } = null!;
	internal readonly DuoArtifactDatabase Database = new();

	private Harmony Harmony { get; set; } = null!;
	private readonly Dictionary<HashSet<string>, ExternalSprite> DuoArtifactSprites = new(HashSet<string>.CreateSetComparer());

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		KokoroApi = contact.GetApi<IKokoroApi>("Shockah.Kokoro")!;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony = new(Name);
		ArtifactPatches.Apply(Harmony);
		ArtifactBrowsePatches.Apply(Harmony);
		ArtifactRewardPatches.Apply(Harmony);
		CharacterPatches.Apply(Harmony);
		CustomTTGlossary.Apply(Harmony);

		foreach (var definition in DuoArtifactDefinition.Definitions)
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.ApplyPatches(Harmony);
	}

	public void FinalizePreperations(IPrelaunchContactPoint contact)
	{
		foreach (var definition in DuoArtifactDefinition.Definitions)
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.ApplyLatePatches(Harmony);
	}

	object? IApiProviderManifest.GetApi(IManifest requestingMod)
		=> new ApiImplementation(Database);

	public void LoadManifest(ISpriteRegistry registry)
	{
		string namePrefix = $"{typeof(ModEntry).Namespace}.Sprite";
		foreach (var definition in DuoArtifactDefinition.Definitions)
		{
			DuoArtifactSprites[definition.CharacterKeys.Value] = registry.RegisterArtOrThrow(
				id: $"{typeof(ModEntry).Namespace}.Artifact.{string.Join("_", definition.CharacterKeys.Value.OrderBy(key => key))}",
				file: new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "Artifacts", $"{definition.AssetName}.png"))
			);
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.RegisterArt(registry, namePrefix, definition);
		}
	}

	public void LoadManifest(IStatusRegistry registry)
	{
		string namePrefix = $"{typeof(ModEntry).Namespace}.Status";
		foreach (var definition in DuoArtifactDefinition.Definitions)
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.RegisterStatuses(registry, namePrefix, definition);
	}

	public void LoadManifest(IDeckRegistry registry)
	{
		Database.DuoArtifactDeck = new(
			globalName: $"{typeof(ModEntry).Namespace}.Deck.Duo",
			deckColor: System.Drawing.Color.White,
			titleColor: System.Drawing.Color.Black,
			cardArtDefault: ExternalSprite.GetRaw((int)StableSpr.cards_colorless),
			borderSprite: ExternalSprite.GetRaw((int)StableSpr.cardShared_border_ephemeral),
			bordersOverSprite: null
		);
		registry.RegisterDeck(Database.DuoArtifactDeck);

		Database.TrioArtifactDeck = new(
			globalName: $"{typeof(ModEntry).Namespace}.Deck.Trio",
			deckColor: System.Drawing.Color.White,
			titleColor: System.Drawing.Color.Black,
			cardArtDefault: ExternalSprite.GetRaw((int)StableSpr.cards_colorless),
			borderSprite: ExternalSprite.GetRaw((int)StableSpr.cardShared_border_ephemeral),
			bordersOverSprite: null
		);
		registry.RegisterDeck(Database.TrioArtifactDeck);

		Database.ComboArtifactDeck = new(
			globalName: $"{typeof(ModEntry).Namespace}.Deck.Combo",
			deckColor: System.Drawing.Color.White,
			titleColor: System.Drawing.Color.Black,
			cardArtDefault: ExternalSprite.GetRaw((int)StableSpr.cards_colorless),
			borderSprite: ExternalSprite.GetRaw((int)StableSpr.cardShared_border_ephemeral),
			bordersOverSprite: null
		);
		registry.RegisterDeck(Database.ComboArtifactDeck);
	}

	public void LoadManifest(IArtifactRegistry registry)
	{
		foreach (var definition in DuoArtifactDefinition.Definitions)
		{
			var deck = definition.Characters.Count switch
			{
				2 => Database.DuoArtifactDeck,
				3 => Database.TrioArtifactDeck,
				_ => Database.ComboArtifactDeck
			};
			ExternalArtifact artifact = new(
				globalName: $"{typeof(ModEntry).Namespace}.Artifact.{string.Join("_", definition.CharacterKeys.Value.OrderBy(key => key))}",
				artifactType: definition.Type,
				sprite: DuoArtifactSprites.GetValueOrDefault(definition.CharacterKeys.Value)!,
				ownerDeck: deck
			);
			artifact.AddLocalisation(definition.Name.ToUpper(), definition.Tooltip);
			registry.RegisterArtifact(artifact);
			Database.RegisterDuoArtifact(definition.Type, definition.Characters);
		}
	}

	public void LoadManifest(ICardRegistry registry)
	{
		string namePrefix = $"{typeof(ModEntry).Namespace}.Card";
		foreach (var definition in DuoArtifactDefinition.Definitions)
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.RegisterCards(registry, namePrefix, definition);
	}

	internal DuoArtifactEligibity GetDuoArtifactEligibity(Deck deck, State state)
	{
		if (state.IsOutsideRun())
			return DuoArtifactEligibity.InvalidState;
		
		var character = state.characters.FirstOrDefault(c => DeckMatches(c.deckType, deck));
		if (character is null)
			return DuoArtifactEligibity.InvalidState;

		DuoArtifactEligibity CheckDetailedEligibity()
		{
			var artifactsForThisCharacter = Database.GetAllDuoArtifactTypes()
				.Select(t => (Type: t, Ownership: Database.GetDuoArtifactTypeOwnership(t)!))
				.Where(e => e.Ownership.Any(owner => DeckMatches(deck, owner)));

			if (!artifactsForThisCharacter.Any())
				return DuoArtifactEligibity.NoDuosForThisCharacter;

			var artifactsForThisCharacterInThisCrew = Database.GetMatchingDuoArtifactTypes(state.characters.Select(c => c.deckType).WhereNotNull())
				.Select(t => (Type: t, Ownership: Database.GetDuoArtifactTypeOwnership(t)!))
				.Where(e => e.Ownership.Any(owner => DeckMatches(deck, owner)));

			if (!artifactsForThisCharacterInThisCrew.Any())
				return DuoArtifactEligibity.NoDuosForThisCrew;

			return DuoArtifactEligibity.Eligible;
		}

		var eligibleArtifacts = character.artifacts
			.Where(a => a.GetMeta().pools.Contains(ArtifactPool.Boss) || !a.GetMeta().unremovable)
			.ToList();

		if (eligibleArtifacts.Count >= ArtifactsRequiredForEligibility)
			return CheckDetailedEligibity();

		var characterCardsInDeck = state.GetAllCards()
			.Where(c => !c.GetDataWithOverrides(state).temporary)
			.Where(c => DB.cardMetas.TryGetValue(c.Key(), out var meta) && !meta.dontOffer && meta.deck == character.deckType)
			.ToList();
		if (characterCardsInDeck.Count >= AnyCardsRequiredForEligibility)
			return CheckDetailedEligibity();

		var rareCharacterCardsInDeck = characterCardsInDeck
			.Where(c => DB.cardMetas.TryGetValue(c.Key(), out var meta) && (int)meta.rarity >= (int)Rarity.rare)
			.ToList();
		if (rareCharacterCardsInDeck.Count >= RareCardsRequiredForEligibility)
			return CheckDetailedEligibity();

		return DuoArtifactEligibity.RequirementsNotSatisfied;
	}

	internal static bool DeckMatches(Deck? lhs, Deck? rhs)
		=> Equals(lhs, rhs) || (lhs == Deck.colorless && rhs == Deck.catartifact) || (lhs == Deck.catartifact && rhs == Deck.colorless);
}
