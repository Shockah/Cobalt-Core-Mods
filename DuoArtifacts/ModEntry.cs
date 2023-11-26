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

namespace Shockah.DuoArtifacts;

public sealed class ModEntry : IModManifest, ISpriteManifest, IArtifactManifest
{
	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal Spr IsaacRiggsArtifactSprite { get; private set; }

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony harmony = new(Name);
		IsaacRiggsArtifact.Apply(harmony);
	}

	public void LoadManifest(ISpriteRegistry artRegistry)
	{
		IsaacRiggsArtifactSprite = artRegistry.RegisterArtOrThrow($"{typeof(ModEntry).Namespace}.Artifact.IsaacRiggs", new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "Artifacts", "IsaacRiggs.png")));
	}

	public void LoadManifest(IArtifactRegistry registry)
	{
		{
			ExternalArtifact artifact = new(
				globalName: $"{typeof(ModEntry).Namespace}.Artifact.IsaacRiggs",
				artifactType: typeof(IsaacRiggsArtifact),
				sprite: ExternalSprite.GetRaw((int)IsaacRiggsArtifactSprite)
			);
			artifact.AddLocalisation("Isaac-Riggs Duo Artifact", "<c=status>EVADE</c> and <c=status>DRONESHIFT</c> can be used interchangeably.\nGain 1 <c=status>EVADE</c> on the first turn.");
			registry.RegisterArtifact(artifact);
		}
	}
}
