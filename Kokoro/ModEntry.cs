using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Pintail;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro;

public sealed class ModEntry : IModManifest, IPrelaunchManifest, IApiProviderManifest, ISpriteManifest, IStatusManifest
{
	internal static readonly string ScorchingTag = $"{typeof(ModEntry).Namespace}.MidrowTag.Scorching";

	public static ModEntry Instance { get; private set; } = null!;
	internal IProxyManager<string> ProxyManager { get; private set; } = null!;
	internal ApiImplementation Api { get; private set; } = null!;
	internal readonly ConditionalWeakTable<object, Dictionary<string, object>> ExtensionDataStorage = new();
	private Harmony Harmony = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal Content Content = new();
	public EvadeManager EvadeManager { get; private init; } = new();
	public DroneShiftManager DroneShiftManager { get; private init; } = new();
	public ArtifactIconManager ArtifactIconManager { get; private init; } = new();
	public MidrowScorchingManager MidrowScorchingManager { get; private init; } = new();
	public WormStatusManager WormStatusManager { get; private init; } = new();
	public OxidationStatusManager OxidationStatusManager { get; private init; } = new();

	internal TimeSpan TotalGameTime;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		var perModModLoaderContactType = AccessTools.TypeByName("CobaltCoreModding.Components.Services.PerModModLoaderContact, CobaltCoreModding.Components");
		var proxyManagerField = AccessTools.DeclaredField(perModModLoaderContactType, "proxyManager");
		ProxyManager = (IProxyManager<string>)proxyManagerField.GetValue(contact)!;
		Api = new();

		Harmony = new(Name);

		ArtifactBrowsePatches.Apply(Harmony);
		ArtifactPatches.Apply(Harmony);
		CardPatches.Apply(Harmony);
		CombatPatches.Apply(Harmony);
		MGPatches.Apply(Harmony);
		ShipPatches.Apply(Harmony);
		StuffBasePatches.Apply(Harmony);

		CustomTTGlossary.Apply(Harmony);

		SetupSerializationChanges();
	}

	public void FinalizePreperations(IPrelaunchContactPoint prelaunchManifest)
	{
		StuffBasePatches.ApplyLate(Harmony);
	}

	public object? GetApi(IManifest requestingMod)
		=> new ApiImplementation();

	public void LoadManifest(ISpriteRegistry registry)
		=> Content.RegisterArt(registry);

	public void LoadManifest(IStatusRegistry registry)
		=> Content.RegisterStatuses(registry);

	private void SetupSerializationChanges()
	{
		JSONSettings.indented.ContractResolver = new ConditionalWeakTableExtensionDataContractResolver(JSONSettings.indented.ContractResolver ?? new DefaultContractResolver(), ExtensionDataStorage);
		JSONSettings.serializer.ContractResolver = new ConditionalWeakTableExtensionDataContractResolver(JSONSettings.serializer.ContractResolver ?? new DefaultContractResolver(), ExtensionDataStorage);
	}
}
