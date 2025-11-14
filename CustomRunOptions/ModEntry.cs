using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;
using TheJazMaster.MoreDifficulties;

namespace Shockah.CustomRunOptions;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal IHarmony Harmony { get; }
	internal readonly IModSettingsApi ModSettingsApi;
	internal IMoreDifficultiesApi? MoreDifficultiesApi { get; private set; }

	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	
	internal Settings Settings { get; private set; }
	
	private static IReadOnlyList<Type> CustomRunOptionTypes { get; } = [
		typeof(SeedCustomRunOption),
		typeof(BootSequenceCustomRunOption),
		typeof(DailyModifiersCustomRunOption),
	];
	
	private static IReadOnlyList<Type> RegisterableTypes { get; } = [
		typeof(CopySeedOnRunSummary),
		typeof(NewRunOptionsButton),
		typeof(PartialCrewRuns),
		typeof(StartRunDetector),
		.. CustomRunOptionTypes,
	];

	internal static readonly IReadOnlyList<ICustomRunOption> CustomRunOptions = CustomRunOptionTypes
		.Select(t => (ICustomRunOption)Activator.CreateInstance(t)!)
		.ToList();
	
	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		ModSettingsApi = helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings")!;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		this.Settings = helper.Storage.LoadJson<Settings>(helper.Storage.GetMainStorageFile("json"));
		
		helper.ModRegistry.AwaitApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties", api => MoreDifficultiesApi = api);

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation(requestingMod);
}