using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json.Serialization;
using Nickel;
using Nickel.Legacy;
using System.Collections.Generic;
using System.IO;
using IModManifest = CobaltCoreModding.Definitions.ModManifests.IModManifest;

namespace Shockah.Kokoro;

public sealed class ModEntry : IModManifest, IApiProviderManifest, ISpriteManifest, IStatusManifest, INickelManifest
{
	internal const string ExtensionDataJsonKey = "KokoroModData";
	internal const string ScorchingTag = "Scorching";

	public static ModEntry Instance { get; private set; } = null!;
	internal IModHelper Helper { get; private set; } = null!;
	internal IPluginPackage<Nickel.IModManifest> Package { get; private set; } = null!;
	internal ApiImplementation Api { get; private set; } = null!;
	internal IHarmony Harmony { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => [];

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal readonly Content Content = new();
	internal readonly ExtensionDataManager ExtensionDataManager = new();

	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; private set; } = null!;
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; private set; } = null!;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		Api = new(this);
	}

	public void OnNickelLoad(IPluginPackage<Nickel.IModManifest> package, IModHelper helper)
	{
		Helper = helper;
		Package = package;
		Harmony = helper.Utilities.Harmony;
		
		AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		ArtifactIconManager.Setup(Harmony);
		CardOfferingAndRewardDestinationManager.Setup(Harmony);
		CardRenderManager.Setup(Harmony);
		ConditionalActionManager.Setup(Harmony);
		ContinueStopActionManager.Setup(Harmony);
		CustomCardBrowseManager.Setup(Harmony);
		CustomCardUpgradeManager.Setup(Harmony);
		DroneShiftManager.Setup(Harmony);
		EnemyStatusVariableHintManager.Setup(Harmony);
		EnergyAsStatusManager.Setup(Harmony);
		EvadeManager.Setup(Harmony);
		HiddenActionManager.Setup(Harmony);
		MultiCardBrowseManager.Setup(Harmony);
		OnDiscardManager.Setup(Harmony);
		OnTurnEndManager.Setup(Harmony);
		PinchCompactFontManager.Setup(Harmony);
		PlaySpecificCardFromAnywhereManager.Setup(Harmony);
		RedrawStatusManager.Setup(Harmony);
		ResourceCostedActionManager.Setup(Harmony);
		SpontaneousManager.Setup(Harmony);
		SpoofedActionManager.Setup(Harmony);
		StatusLogicManager.Setup(Harmony);
		StatusRenderManager.Setup(Harmony);
		TemporaryUpgradesManager.Setup(Harmony);
		TimesPlayedManager.Setup(Harmony);
		WrappedActionManager.Setup(Harmony);
		
		ConditionalActionManager.SetupLate();
		ContinueStopActionManager.SetupLate();
		HiddenActionManager.SetupLate();
		OnDiscardManager.SetupLate();
		OnTurnEndManager.SetupLate();
		ResourceCostedActionManager.SetupLate();
		SpontaneousManager.SetupLate();
		SpoofedActionManager.SetupLate();
		
		StatusLogicManager.Instance.Register(WormStatusManager.Instance, 0);
		StatusLogicManager.Instance.Register(OxidationStatusManager.Instance, 0);
		StatusLogicManager.Instance.Register(StatusNextTurnManager.Instance, 0);
		StatusRenderManager.Instance.Register(WormStatusManager.Instance, 0);
		StatusRenderManager.Instance.Register(OxidationStatusManager.Instance, 0);
		StatusRenderManager.Instance.Register(StatusNextTurnManager.Instance, 0);

		SetupSerializationChanges();
	}

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
			JSONSettings.serializer.ContractResolver ?? new DefaultContractResolver(),
			ExtensionDataJsonKey,
			ExtensionDataManager
		);
	}
}
