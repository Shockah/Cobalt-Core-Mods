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

namespace Shockah.Wormholes;

public sealed class ModEntry : IModManifest, ISpriteManifest
{
	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal ExternalSprite WormholeSprite { get; private set; } = null!;

	internal WormholeMode WormholeMode = WormholeMode.None;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony harmony = new(Name);
		EventsPatches.Apply(harmony);
		MapBasePatches.Apply(harmony);
		MapRoutePatches.Apply(harmony);
	}

	public void LoadManifest(ISpriteRegistry registry)
	{
		WormholeSprite = registry.RegisterArtOrThrow(
			id: $"{typeof(ModEntry).Namespace}.Icon.Wormhole",
			file: new FileInfo(Path.Combine(ModRootFolder!.FullName, "assets", "Wormhole.png"))
		);
	}
}
