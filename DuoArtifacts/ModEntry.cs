using CobaltCoreModding.Definitions;
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

public sealed class ModEntry : IModManifest, ISpriteManifest, IGlossaryManifest, IDeckManifest, IArtifactManifest
{
	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[] { new DependencyEntry<IModManifest>("Shockah.Kokoro", ignoreIfMissing: false) };

	internal const ExternalGlossary.GlossayType StatusGlossaryType = (ExternalGlossary.GlossayType)2137001;

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal ExternalDeck DuoArtifactsDeck { get; private set; } = null!;
	internal string FluxAltGlossaryKey { get; private set; } = null!;
	internal IKokoroApi KokoroApi { get; set; } = null!;

	internal TimeSpan TotalGameTime;

	private readonly Dictionary<HashSet<string>, ExternalSprite> DuoArtifactSprites = new(HashSet<string>.CreateSetComparer());
	private readonly Dictionary<HashSet<string>, Type> DuoArtifactTypes = new(HashSet<string>.CreateSetComparer());

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		KokoroApi = contact.GetApi<IKokoroApi>("Shockah.Kokoro")!;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony harmony = new(Name);
		ArtifactRewardPatches.Apply(harmony);
		CharacterPatches.Apply(harmony);
		EnumPatches.Apply(harmony);
		MGPatches.Apply(harmony);

		foreach (var definition in DuoArtifactDefinition.Definitions)
			(Activator.CreateInstance(definition.Type) as DuoArtifact)?.ApplyPatches(harmony);
	}

	public void LoadManifest(ISpriteRegistry artRegistry)
	{
		foreach (var definition in DuoArtifactDefinition.Definitions)
			DuoArtifactSprites[definition.CharacterKeys.Value] = artRegistry.RegisterArtOrThrow(
				id: $"{typeof(ModEntry).Namespace}.Artifact.{string.Join("_", definition.CharacterKeys.Value.OrderBy(key => key))}",
				file: new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "Artifacts", $"{definition.AssetName}.png"))
			);
	}

	public void LoadManifest(IGlossaryRegisty registry)
	{
		{
			ExternalGlossary glossary = new($"{typeof(ModEntry).Namespace}.Glossary.Flux.Alt", "Flux", false, StatusGlossaryType, ExternalSprite.GetRaw((int)Spr.icons_libra));
			glossary.AddLocalisation("en", I18n.FluxAltGlossaryName, I18n.FluxAltGlossaryDescription);
			registry.RegisterGlossary(glossary);
			FluxAltGlossaryKey = glossary.Head;
		}
	}

	public void LoadManifest(IDeckRegistry registry)
	{
		DuoArtifactsDeck = new(
			globalName: $"{typeof(ModEntry).Namespace}.Deck.Duo",
			deckColor: System.Drawing.Color.White,
			titleColor: System.Drawing.Color.White,
			cardArtDefault: ExternalDeck.GetRaw((int)Deck.colorless).CardArtDefault,
			borderSprite: ExternalDeck.GetRaw((int)Deck.colorless).BorderSprite,
			bordersOverSprite: ExternalDeck.GetRaw((int)Deck.colorless).BordersOverSprite
		);
		registry.RegisterDeck(DuoArtifactsDeck);
	}

	public void LoadManifest(IArtifactRegistry registry)
	{
		foreach (var definition in DuoArtifactDefinition.Definitions)
		{
			ExternalArtifact artifact = new(
				globalName: $"{typeof(ModEntry).Namespace}.Artifact.{string.Join("_", definition.CharacterKeys.Value.OrderBy(key => key))}",
				artifactType: definition.Type,
				sprite: DuoArtifactSprites.GetValueOrDefault(definition.CharacterKeys.Value)!,
				ownerDeck: DuoArtifactsDeck
			);
			artifact.AddLocalisation(definition.Name, definition.Tooltip);
			registry.RegisterArtifact(artifact);
			DuoArtifactTypes[definition.CharacterKeys.Value] = definition.Type;
		}
	}

	public Type? GetDuoArtifactType(IEnumerable<Deck> characters)
		=> DuoArtifactTypes.TryGetValue(characters.Select(d => d.Key()).ToHashSet(), out var artifactType) ? artifactType : null;

	public Artifact? InstantiateDuoArtifact(IEnumerable<Deck> characters)
	{
		var type = GetDuoArtifactType(characters);
		return type is null ? null : (Artifact)Activator.CreateInstance(type)!;
	}

	public List<Artifact> InstantiateDuoArtifacts(IEnumerable<Deck> characters)
	{
		List<Artifact> results = new();
		foreach (var firstCharacter in characters)
		{
			foreach (var secondCharacter in characters)
			{
				if (secondCharacter == firstCharacter)
					continue;

				var artifact = InstantiateDuoArtifact(new Deck[] { firstCharacter, secondCharacter });
				if (artifact is not null)
					results.Add(artifact);
			}
		}
		return results;
	}

	public IReadOnlySet<Deck>? GetCharactersForDuoArtifact(Type artifactType)
	{
		var characterKeys = DuoArtifactTypes.FirstOrNull(kvp => kvp.Value == artifactType)?.Key;
		return characterKeys is null ? null : DB.decks.Where(kvp => characterKeys.Contains(kvp.Key.Key())).Select(kvp => kvp.Key).ToHashSet();
	}

	public IReadOnlySet<Deck>? GetCharactersForDuoArtifact(DuoArtifact artifact)
		=> GetCharactersForDuoArtifact(artifact.GetType());

	public ExternalSprite? GetSpriteForDuoArtifact(DuoArtifact artifact)
	{
		var characterKeys = DuoArtifactTypes.FirstOrNull(kvp => kvp.Value == artifact.GetType())?.Key;
		if (characterKeys is null)
			return null;
		return DuoArtifactSprites.GetValueOrDefault(characterKeys);
	}
}
