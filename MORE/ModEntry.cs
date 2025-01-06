using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;
using Nickel.Essentials;

namespace Shockah.MORE;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal Harmony Harmony { get; }
	internal IKokoroApi.IV2 KokoroApi { get; }
	internal IEssentialsApi EssentialsApi { get; }

	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal Settings Settings { get; private set; }

	internal readonly HashSet<string> AltruisticArtifactKeys = [
		// Dizzy
		nameof(ReboundReagent),
		nameof(ShieldReserves),
		nameof(ShieldBurst),
		nameof(Prototype22),

		// Riggs
		nameof(Quickdraw),
		nameof(PerpetualMotionDevice),
		nameof(CaffeineRush),
		nameof(DemonThrusters),
		nameof(Flywheel),

		// Peri
		nameof(RevengeDrive),
		nameof(Premeditation),
		nameof(BerserkerDrive),

		// Max
		nameof(SafetyLock),
		nameof(StickyNote),
		nameof(StrongStart),
		nameof(RightClickArtifact),
		nameof(FlowState),
		nameof(TridimensionalCockpit),
		nameof(LightspeedBootDisk),

		// CAT
		nameof(StandbyMode),
		nameof(InitialBooster),
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

	internal static IReadOnlyList<Type> ArtifactTypes { get; } = [
		// CAT
		typeof(PatchNotesArtifact),
	];

	internal static IEnumerable<Type> RegisterableTypes { get; }
		= [
			.. StatusTypes,
			.. EnemyTypes,
			.. EventTypes,
			.. ArtifactTypes,
			typeof(EphemeralUpgrades),
			typeof(ReleaseUpgrades),
			typeof(CardSelectFilters),
			typeof(ToothCards),
			//typeof(BootSequenceDownsides),
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;
		EssentialsApi = helper.ModRegistry.GetApi<IEssentialsApi>("Nickel.Essentials")!;

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

		helper.Events.OnSaveLoaded += (_, _) => UpdateSettings();

		helper.ModRegistry.AwaitApi<IModSettingsApi>(
			"Nickel.ModSettings",
			api => api.RegisterModSettings(api.MakeList([
				api.MakeProfileSelector(
					() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
					Settings.ProfileBased
				),
				api.MakeButton(
					() => Localizations.Localize(["settings", "artifacts", "name"]),
					(g, route) => route.OpenSubroute(g, api.MakeModSettingsRoute(api.MakeList([
						api.MakeHeader(
							() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
							() => Localizations.Localize(["settings", "artifacts", "name"])
						),
						api.MakeList([
							api.MakeCheckbox(
								() => Localizations.Localize(["settings", "artifacts", "values", "CAT", "PatchNotes"]),
								() => !Settings.ProfileBased.Current.DisabledArtifacts.Contains(PatchNotesArtifact.ArtifactEntry.UniqueName),
								(_, _, value) =>
								{
									if (value)
										Settings.ProfileBased.Current.DisabledArtifacts.Remove(PatchNotesArtifact.ArtifactEntry.UniqueName);
									else
										Settings.ProfileBased.Current.DisabledArtifacts.Add(PatchNotesArtifact.ArtifactEntry.UniqueName);
								}
							).SetTooltips(() => new PatchNotesArtifact().GetTooltips())
						]),
						api.MakeBackButton()
					]).SetSpacing(8)))
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
								.Select(IModSettingsApi.IModSetting (e) => api.MakeCheckbox(
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
				api.MakeButton(
					() => Localizations.Localize(["settings", "toothCards", "name"]),
					(g, route) => route.OpenSubroute(g, api.MakeModSettingsRoute(api.MakeList([
						api.MakeHeader(
							() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
							() => Localizations.Localize(["settings", "toothCards", "name"])
						),
						api.MakeList(
							ToothCards.AllToothCardKeys
								.Where(key => DB.cards.ContainsKey(key))
								.Select(IModSettingsApi.IModSetting (key) => api.MakeCheckbox(
									() => Loc.T($"card.{key}.name"),
									() => !Settings.ProfileBased.Current.DisabledToothCards.Contains(key),
									(_, _, value) =>
									{
										if (value)
											Settings.ProfileBased.Current.DisabledToothCards.Remove(key);
										else
											Settings.ProfileBased.Current.DisabledToothCards.Add(key);
									}
								).SetTooltips(() => [new TTCard { card = (Card)Activator.CreateInstance(DB.cards[key])! }]))
								.ToList()
						),
						api.MakeBackButton()
					]).SetSpacing(8)))
				).SetValueText(
					() => $"{ToothCards.AllToothCardKeys.Length - Settings.ProfileBased.Current.DisabledToothCards.Count}/{ToothCards.AllToothCardKeys.Length}"
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{package.Manifest.UniqueName}::{nameof(ProfileSettings.DisabledToothCards)}")
					{
						TitleColor = Colors.textBold,
						Title = Localizations.Localize(["settings", "toothCards", "name"]),
						Description = Localizations.Localize(["settings", "toothCards", "description"])
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

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation();
}
