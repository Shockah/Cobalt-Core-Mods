using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.ABUpgrades;

public sealed class ModEntry : IModManifest, IPrelaunchManifest, IApiProviderManifest
{
	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	private Harmony Harmony { get; set; } = null!;
	internal ABUpgradeManager Manager { get; private set; } = null!;
	internal ApiImplementation Api { get; private set; } = null!;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Manager = new();
		Api = new();

		Harmony = new(Name);
		CardPatches.Apply(Harmony);
		DBExtenderPatches.Apply(Harmony);

		PeriUpgrades.RegisterUpgrades(Api);
	}

	public void FinalizePreperations(IPrelaunchContactPoint prelaunchManifest)
	{
		CardPatches.ApplyLate(Harmony);
	}

	public object? GetApi(IManifest requestingMod)
		=> new ApiImplementation();
}
