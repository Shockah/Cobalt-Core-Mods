﻿using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.MORE;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal Harmony Harmony { get; }
	internal IKokoroApi KokoroApi { get; }

	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal Settings Settings { get; private set; } = new();

	internal readonly HashSet<string> AltruisticArtifactKeys = [
		// Dizzy
		typeof(ReboundReagent).Name,
		typeof(ShieldReserves).Name,
		typeof(ShieldBurst).Name,
		typeof(Prototype22).Name,

		// Riggs
		typeof(Quickdraw).Name,
		typeof(PerpetualMotionDevice).Name,
		typeof(CaffeineRush).Name,
		typeof(DemonThrusters).Name,
		typeof(Flywheel).Name,

		// Peri
		typeof(RevengeDrive).Name,
		typeof(Premeditation).Name,
		typeof(BerserkerDrive).Name,

		// Max
		typeof(SafetyLock).Name,
		typeof(StickyNote).Name,
		typeof(StrongStart).Name,
		typeof(RightClickArtifact).Name,
		typeof(FlowState).Name,
		typeof(TridimensionalCockpit).Name,
		typeof(LightspeedBootDisk).Name,

		// CAT
		typeof(StandbyMode).Name,
		typeof(InitialBooster).Name,
	];

	internal static IReadOnlyList<Type> StatusTypes { get; } = [
		typeof(ActionReactionStatus),
		//typeof(BombEnemy.SelfDestructTimerStatus),
		typeof(VolatileOverdriveStatus),
	];

	internal static IReadOnlyList<Type> EnemyTypes { get; } = [
		//typeof(ActionReactionEnemy),
		//typeof(BombEnemy),
		//typeof(VolatileOverdriveEnemy),
	];

	internal static IReadOnlyList<Type> EventTypes { get; } = [
		typeof(AbyssalPowerEvent),
		typeof(CombatDataCalibrationEvent),
		typeof(DraculaDeckTrialEvent),
		typeof(ShipSwapEvent),
	];

	internal static IEnumerable<Type> RegisterableTypes { get; }
		= [
			..StatusTypes,
			..EnemyTypes,
			..EventTypes,
			typeof(EphemeralUpgrades),
			typeof(ReleaseUpgrades),
			typeof(CardSelectFilters),
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		this.Settings = helper.Storage.LoadJson<Settings>(helper.Storage.GetMainStorageFile("json"));

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
		UpdateSettings();

		helper.ModRegistry.AwaitApi<IModSettingsApi>(
			"Nickel.ModSettings",
			api => api.RegisterModSettings(api.MakeList([
				api.MakeProfileSelector(
					() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
					Settings.ProfileBased
				),
				api.MakeButton(
					() => Localizations.Localize(["settings", "events", "name"]),
					(g, route) => route.OpenSubroute(g, api.MakeModSettingsRoute(api.MakeList([
						api.MakeHeader(
							() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
							() => Localizations.Localize(["settings", "events", "name"])
						),
						api.MakeList(
							Enum.GetValues<MoreEvent>()
								.Select(e => (IModSettingsApi.IModSetting)api.MakeCheckbox(
									() => Localizations.Localize(["settings", "events", "values", e.ToString()]),
									() => !Settings.ProfileBased.Current.DisabledEvents.Contains(e),
									(_, _, value) =>
									{
										if (value)
											Settings.ProfileBased.Current.DisabledEvents.Remove(e);
										else
											Settings.ProfileBased.Current.DisabledEvents.Add(e);
									}
								))
								.ToList()
						),
						api.MakeBackButton()
					]).SetSpacing(8)))
				),
				api.MakeCheckbox(
					() => Localizations.Localize(["settings", "ephemeralUpgrades", "name"]),
					() => Settings.ProfileBased.Current.EnabledEphemeralUpgrades,
					(_, _, value) => Settings.ProfileBased.Current.EnabledEphemeralUpgrades = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.EnabledEphemeralUpgrades)}")
					{
						TitleColor = Colors.textBold,
						Title = Localizations.Localize(["settings", "ephemeralUpgrades", "name"]),
						Description = Localizations.Localize(["settings", "ephemeralUpgrades", "description"])
					}
				]),
				api.MakeCheckbox(
					() => Localizations.Localize(["settings", "releaseUpgrades", "name"]),
					() => Settings.ProfileBased.Current.EnabledReleaseUpgrades,
					(_, _, value) => Settings.ProfileBased.Current.EnabledReleaseUpgrades = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.EnabledReleaseUpgrades)}")
					{
						TitleColor = Colors.textBold,
						Title = Localizations.Localize(["settings", "releaseUpgrades", "name"]),
						Description = Localizations.Localize(["settings", "releaseUpgrades", "description"])
					}
				]),
				api.MakeCheckbox(
					() => Localizations.Localize(["settings", "flippableRelease", "name"]),
					() => Settings.ProfileBased.Current.EnabledFlippableRelease,
					(_, _, value) => Settings.ProfileBased.Current.EnabledFlippableRelease = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.EnabledFlippableRelease)}")
					{
						TitleColor = Colors.textBold,
						Title = Localizations.Localize(["settings", "flippableRelease", "name"]),
						Description = Localizations.Localize(["settings", "flippableRelease", "description"])
					}
				])
			]).SubscribeToOnMenuClose(_ =>
			{
				helper.Storage.SaveJson(helper.Storage.GetMainStorageFile("json"), Settings);
				UpdateSettings();
			}))
		);
	}

	private void UpdateSettings()
	{
		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.UpdateSettings))?.Invoke(null, [Package, Helper, Settings.ProfileBased.Current]);
	}

	public override object? GetApi(IModManifest requestingMod)
		=> new ApiImplementation();
}