using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CustomRunOptions;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal IHarmony Harmony { get; }
	internal readonly IModSettingsApi ModSettingsApi;

	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	
	private static IReadOnlyList<Type> CustomRunOptionTypes { get; } = [
		typeof(DailyModifiersCustomRunOption),
	];
	
	private static IReadOnlyList<Type> RegisterableTypes { get; } = [
		typeof(NewRunOptionsButton),
		typeof(SoloRuns),
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

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}
}