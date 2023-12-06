using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Shockah.Rerolls.Patches;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Rerolls;

public sealed class ModEntry : IModManifest, ISpriteManifest, IArtifactManifest
{
	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal ExternalSprite RerollArtifactSprite { get; private set; } = null!;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony harmony = new(Name);
		ArtifactRewardPatches.Apply(harmony);
		CardRewardPatches.Apply(harmony);
		EventsPatches.Apply(harmony);
		MapExitPatches.Apply(harmony);
		StatePatches.Apply(harmony);
	}

	public void LoadManifest(ISpriteRegistry registry)
	{
		RerollArtifactSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Artifact.Reroll",
			file: new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "RerollArtifact.png"))
		);
	}

	public void LoadManifest(IArtifactRegistry registry)
	{
		ExternalArtifact artifact = new(
			globalName: $"{typeof(ModEntry).Namespace}.Artifact.Reroll",
			artifactType: typeof(RerollArtifact),
			sprite: RerollArtifactSprite,
			ownerDeck: ExternalDeck.GetRaw((int)Deck.colorless)
		);
		artifact.AddLocalisation(I18n.ArtifactName.ToUpper(), I18n.ArtifactTooltip);
		registry.RegisterArtifact(artifact);
	}
}
