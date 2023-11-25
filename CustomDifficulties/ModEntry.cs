using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.CustomDifficulties;

public sealed class ModEntry : IModManifest, ISpriteManifest
{
	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public const int EasyDifficultyLevel = -1;

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }


	internal Spr EasyModeArtifactSprite { get; private set; }

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony harmony = new(Name);
		ArtifactPatches.Apply(harmony);
		HardmodePatches.Apply(harmony);
		NewRunOptionsPatches.Apply(harmony);
		RunConfigPatches.Apply(harmony);
		StatePatches.Apply(harmony);
	}

	public void LoadManifest(ISpriteRegistry artRegistry)
	{
		EasyModeArtifactSprite = artRegistry.RegisterArtOrThrow($"{typeof(ModEntry).Namespace}.Artifact.EasyMode", new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "Artifact-EasyMode.png")));
	}
}
