using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Kokoro;

public sealed class ModEntry : IModManifest, IPrelaunchManifest, IApiProviderManifest
{
	internal static readonly string ScorchingTag = $"{typeof(ModEntry).Namespace}.MidrowTag.Scorching";

	public static ModEntry Instance { get; private set; } = null!;
	internal readonly ApiImplementation Api = new();
	private Harmony Harmony = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	public EvadeHookManager EvadeHookManager { get; private init; } = new();
	public DroneShiftHookManager DroneShiftHookManager { get; private init; } = new();
	public ArtifactIconHookManager ArtifactIconHookManager { get; private init; } = new();
	public HookManager<IMidrowScorchingHook> MidrowScorchingHookManager { get; private init; } = new();

	internal TimeSpan TotalGameTime;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony = new(Name);

		ArtifactBrowsePatches.Apply(Harmony);
		ArtifactPatches.Apply(Harmony);
		CombatPatches.Apply(Harmony);
		MGPatches.Apply(Harmony);
		ShipPatches.Apply(Harmony);
		StuffBasePatches.Apply(Harmony);

		CustomTTGlossary.Apply(Harmony);
	}

	public void FinalizePreperations(IPrelaunchContactPoint prelaunchManifest)
	{
		StuffBasePatches.ApplyLate(Harmony);
	}

	public object? GetApi(IManifest requestingMod)
		=> new ApiImplementation();
}
