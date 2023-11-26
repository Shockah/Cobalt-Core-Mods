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

public sealed class ModEntry : IModManifest, ISpriteManifest, IDeckManifest, IArtifactManifest
{
	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal ExternalSprite IsaacRiggsArtifactSprite { get; private set; } = null!;

	internal ExternalDeck DuoArtifactsDeck { get; private set; } = null!;

	internal TimeSpan TotalGameTime;

	private readonly Dictionary<HashSet<string>, Type> DuoArtifactTypes = new(HashSet<string>.CreateSetComparer());

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony harmony = new(Name);
		MGPatches.Apply(harmony);
		ArtifactRewardPatches.Apply(harmony);
		CharacterPatches.Apply(harmony);

		IsaacRiggsArtifact.Apply(harmony);
	}

	public void LoadManifest(ISpriteRegistry artRegistry)
	{
		IsaacRiggsArtifactSprite = artRegistry.RegisterArtOrThrow($"{typeof(ModEntry).Namespace}.Artifact.IsaacRiggs", new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "Artifacts", "IsaacRiggs.png")));
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
		void Register(IEnumerable<Deck> characters, Type artifactType, ExternalSprite sprite, string name, string description)
		{
			var sortedCharacterKeys = characters.Select(d => d.Key()).OrderBy(key => key).ToList();
			ExternalArtifact artifact = new(
				globalName: $"{typeof(ModEntry).Namespace}.Artifact.{string.Join("_", sortedCharacterKeys)}",
				artifactType: artifactType,
				sprite: sprite,
				ownerDeck: DuoArtifactsDeck
			);
			artifact.AddLocalisation(name, description);
			registry.RegisterArtifact(artifact);
			DuoArtifactTypes[sortedCharacterKeys.ToHashSet()] = artifactType;
		}

		Register(new Deck[] { Deck.riggs, Deck.goat }, typeof(IsaacRiggsArtifact), IsaacRiggsArtifactSprite, I18n.IsaacRiggsArtifactName, I18n.IsaacRiggsArtifactTooltip);
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
}
