using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json.Serialization;
using Nickel.Legacy;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.Kokoro;

public sealed class ModEntry : IModManifest, IPrelaunchManifest, IApiProviderManifest, ISpriteManifest, IStatusManifest, INickelManifest
{
	internal const string ExtensionDataJsonKey = "KokoroModData";
	internal const string ScorchingTag = "Scorching";

	public static ModEntry Instance { get; private set; } = null!;
	internal Nickel.IModHelper Helper { get; private set; } = null!;
	internal ApiImplementation Api { get; private set; } = null!;
	private Nickel.IHarmony Harmony = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => [];

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal readonly Content Content = new();
	internal readonly ExtensionDataManager ExtensionDataManager = new();
	public readonly EvadeManager EvadeManager = new();
	public readonly DroneShiftManager DroneShiftManager = new();
	public readonly ArtifactIconManager ArtifactIconManager = new();
	public readonly StatusRenderManager StatusRenderManager = new();
	public readonly StatusLogicManager StatusLogicManager = new();
	public readonly CardRenderManager CardRenderManager = new();
	public readonly WrappedActionManager WrappedActionManager = new();
	public readonly MidrowScorchingManager MidrowScorchingManager = new();
	public readonly WormStatusManager WormStatusManager = new();
	public readonly OxidationStatusManager OxidationStatusManager = new();
	public readonly RedrawStatusManager RedrawStatusManager = new();
	public readonly StatusNextTurnManager StatusNextTurnManager = new();
	public readonly CustomCardBrowseManager CustomCardBrowseManager = new();

	internal TimeSpan TotalGameTime;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		Api = new(this);

		StatusLogicManager.Register(WormStatusManager, 0);
		StatusLogicManager.Register(OxidationStatusManager, 0);
		StatusLogicManager.Register(StatusNextTurnManager, 0);
		StatusRenderManager.Register(OxidationStatusManager, 0);
		StatusRenderManager.Register(StatusNextTurnManager, 0);
	}

	public void OnNickelLoad(IPluginPackage<Nickel.IModManifest> package, Nickel.IModHelper helper)
	{
		Helper = helper;
		Harmony = helper.Utilities.Harmony;

		ACardOfferingPatches.Apply(Harmony);
		ACardSelectPatches.Apply(Harmony);
		ArtifactBrowsePatches.Apply(Harmony);
		ArtifactPatches.Apply(Harmony);
		AStatusPatches.Apply(Harmony);
		AVariableHintPatches.Apply(Harmony);
		BigStatsPatches.Apply(Harmony);
		CardBrowsePatches.Apply(Harmony);
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
		=> StuffBasePatches.ApplyLate(Harmony);

	public object GetApi(IManifest requestingMod)
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
			ExtensionDataManager
		);
		JSONSettings.serializer.ContractResolver = new ConditionalWeakTableExtensionDataContractResolver(
			// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
			JSONSettings.serializer.ContractResolver ?? new DefaultContractResolver(),
			ExtensionDataJsonKey,
			ExtensionDataManager
		);
	}
}
