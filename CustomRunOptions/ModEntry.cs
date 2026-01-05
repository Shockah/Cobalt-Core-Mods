using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Essentials;
using Nickel.ModSettings;
using TheJazMaster.MoreDifficulties;

namespace Shockah.CustomRunOptions;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal IHarmony Harmony { get; }
	internal readonly IModSettingsApi ModSettingsApi;
	internal IMoreDifficultiesApi? MoreDifficultiesApi { get; private set; }
	internal IEssentialsApi? EssentialsApi { get; private set; }

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

	internal readonly OrderedList<ICustomRunOptionsApi.ICustomRunOption, double> CustomRunOptions = new(ascending: false);
	
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
		
		helper.ModRegistry.AwaitApi<IEssentialsApi>("Nickel.Essentials", api => EssentialsApi = api);
		helper.ModRegistry.AwaitApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties", api => MoreDifficultiesApi = api);

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
		
		ModSettingsApi.RegisterModSettings(ModSettingsApi.MakeList([
			ModSettingsApi.MakeProfileSelector(
				() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
				Settings.ProfileBased
			),
			ModSettingsApi.MakeNumericStepper(
				() => Localizations.Localize(["settings", nameof(ProfileSettings.UnmannedDailyChance), "title"]),
				() => Settings.ProfileBased.Current.UnmannedDailyChance,
				value => Settings.ProfileBased.Current.UnmannedDailyChance = Math.Round(value, 2),
				minValue: 0,
				maxValue: 1,
				step: 0.01
			).SetValueFormatter(value => value.ToString("F2")).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.UnmannedDailyChance)}")
				{
					TitleColor = Colors.textBold,
					Title = Localizations.Localize(["settings", nameof(ProfileSettings.UnmannedDailyChance), "title"]),
					Description = Localizations.Localize(["settings", nameof(ProfileSettings.UnmannedDailyChance), "description"]),
				},
			]),
			ModSettingsApi.MakeNumericStepper(
				() => Localizations.Localize(["settings", nameof(ProfileSettings.SoloDailyChance), "title"]),
				() => Settings.ProfileBased.Current.SoloDailyChance,
				value => Settings.ProfileBased.Current.SoloDailyChance = Math.Round(value, 2),
				minValue: 0,
				maxValue: 1,
				step: 0.01
			).SetValueFormatter(value => value.ToString("F2")).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.SoloDailyChance)}")
				{
					TitleColor = Colors.textBold,
					Title = Localizations.Localize(["settings", nameof(ProfileSettings.SoloDailyChance), "title"]),
					Description = Localizations.Localize(["settings", nameof(ProfileSettings.SoloDailyChance), "description"]),
				},
			]),
			ModSettingsApi.MakeNumericStepper(
				() => Localizations.Localize(["settings", nameof(ProfileSettings.DuoDailyChance), "title"]),
				() => Settings.ProfileBased.Current.DuoDailyChance,
				value => Settings.ProfileBased.Current.DuoDailyChance = Math.Round(value, 2),
				minValue: 0,
				maxValue: 1,
				step: 0.01
			).SetValueFormatter(value => value.ToString("F2")).SetTooltips(() => [
				new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.DuoDailyChance)}")
				{
					TitleColor = Colors.textBold,
					Title = Localizations.Localize(["settings", nameof(ProfileSettings.DuoDailyChance), "title"]),
					Description = Localizations.Localize(["settings", nameof(ProfileSettings.DuoDailyChance), "description"]),
				},
			]),
		]).SubscribeToOnMenuClose(_ =>
		{
			helper.Storage.SaveJson(helper.Storage.GetMainStorageFile("json"), Settings);
		}));
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation(requestingMod);
}