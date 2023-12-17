using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Pintail;
using Newtonsoft.Json.Serialization;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Kokoro;

public sealed class ModEntry : IModManifest, IPrelaunchManifest, IApiProviderManifest, ISpriteManifest, IStatusManifest
{
	internal const string ExtensionDataJsonKey = "KokoroModData";
	internal const string ScorchingTag = "Scorching";

	public static ModEntry Instance { get; private set; } = null!;
	internal IProxyManager<string> ProxyManager { get; private set; } = null!;
	internal ApiImplementation Api { get; private set; } = null!;
	private Harmony Harmony = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal Content Content = new();
	internal ExtensionDataManager ExtensionDataManager { get; private init; } = new();
	public EvadeManager EvadeManager { get; private init; } = new();
	public DroneShiftManager DroneShiftManager { get; private init; } = new();
	public ArtifactIconManager ArtifactIconManager { get; private init; } = new();
	public StatusRenderManager StatusRenderManager { get; private init; } = new();
	public StatusLogicManager StatusLogicManager { get; private init; } = new();
	public CardRenderManager CardRenderManager { get; private init; } = new();
	public WrappedActionManager WrappedActionManager { get; private init; } = new();
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
		Api = new(this);
		Api.RegisterTypeForExtensionData(typeof(StuffBase));

		Harmony = new(Name);

		ArtifactBrowsePatches.Apply(Harmony);
		ArtifactPatches.Apply(Harmony);
		AStatusPatches.Apply(Harmony);
		CardPatches.Apply(Harmony);
		CombatPatches.Apply(Harmony);
		EditorPatches.Apply(Harmony);
		MGPatches.Apply(Harmony);
		RevengeDrivePatches.Apply(Harmony);
		SecondOpinionsPatches.Apply(Harmony);
		ShipPatches.Apply(Harmony);
		StatusMetaPatches.Apply(Harmony);
		StuffBasePatches.Apply(Harmony);

		CustomTTGlossary.ApplyPatches(Harmony);
		APlaySpecificCardFromAnywhere.ApplyPatches(Harmony);

		SetupSerializationChanges();
	}

	public void FinalizePreperations(IPrelaunchContactPoint prelaunchManifest)
	{
		StuffBasePatches.ApplyLate(Harmony);
	}

	public object? GetApi(IManifest requestingMod)
		=> new ApiImplementation(requestingMod);

	public void LoadManifest(ISpriteRegistry registry)
		=> Content.RegisterArt(registry);

	public void LoadManifest(IStatusRegistry registry)
		=> Content.RegisterStatuses(registry);

	private void SetupSerializationChanges()
	{
		JSONSettings.indented.ContractResolver = new ConditionalWeakTableExtensionDataContractResolver(
			JSONSettings.indented.ContractResolver ?? new DefaultContractResolver(),
			ExtensionDataJsonKey,
			ExtensionDataManager.ExtensionDataStorage,
			ExtensionDataManager.IsTypeRegisteredForExtensionData
		);
		JSONSettings.serializer.ContractResolver = new ConditionalWeakTableExtensionDataContractResolver(
			JSONSettings.serializer.ContractResolver ?? new DefaultContractResolver(),
			ExtensionDataJsonKey,
			ExtensionDataManager.ExtensionDataStorage,
			ExtensionDataManager.IsTypeRegisteredForExtensionData
		);
	}
}
