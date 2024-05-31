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
using System.Reflection;
using System.Reflection.Emit;

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
	public IEnumerable<DependencyEntry> Dependencies => [];

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
	public RedrawStatusManager RedrawStatusManager { get; private init; } = new();
	public StatusNextTurnManager StatusNextTurnManager { get; private init; } = new();

	internal TimeSpan TotalGameTime;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Pintail.dll"));

		AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"CobaltCoreModding.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
		ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("CobaltCoreModding.Proxies");
		ProxyManager = new ProxyManager<string>(moduleBuilder, new ProxyManagerConfiguration<string>(
			proxyPrepareBehavior: ProxyManagerProxyPrepareBehavior.Eager,
			proxyObjectInterfaceMarking: ProxyObjectInterfaceMarking.MarkerWithProperty,
			accessLevelChecking: AccessLevelChecking.DisabledButOnlyAllowPublicMembers
		));
		Api = new(this);
		Api.RegisterTypeForExtensionData(typeof(ACardOffering));
		Api.RegisterTypeForExtensionData(typeof(AStatus));
		Api.RegisterTypeForExtensionData(typeof(AVariableHint));
		Api.RegisterTypeForExtensionData(typeof(CardReward));
		Api.RegisterTypeForExtensionData(typeof(Combat));
		Api.RegisterTypeForExtensionData(typeof(StuffBase));

		StatusLogicManager.Register(WormStatusManager, 0);
		StatusLogicManager.Register(OxidationStatusManager, 0);
		StatusLogicManager.Register(StatusNextTurnManager, 0);
		StatusRenderManager.Register(OxidationStatusManager, 0);
		StatusRenderManager.Register(StatusNextTurnManager, 0);

		Harmony = new(Name);

		ACardOfferingPatches.Apply(Harmony);
		ArtifactBrowsePatches.Apply(Harmony);
		ArtifactPatches.Apply(Harmony);
		AStatusPatches.Apply(Harmony);
		AVariableHintPatches.Apply(Harmony);
		BigStatsPatches.Apply(Harmony);
		CardPatches.Apply(Harmony);
		CardRewardPatches.Apply(Harmony);
		CombatPatches.Apply(Harmony);
		DrawPatches.Apply(Harmony);
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
