using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;

namespace Shockah.Rerolls;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly Harmony Harmony;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	internal readonly Settings Settings;

	private IWritableFileInfo SettingsFile
		=> this.Helper.Storage.GetMainStorageFile("json");

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);
		this.Settings = helper.Storage.LoadJson<Settings>(this.SettingsFile);

		helper.Content.Artifacts.RegisterArtifact("Rerolls", new()
		{
			ArtifactType = typeof(RerollArtifact),
			Meta = new()
			{
				owner = Deck.colorless,
				unremovable = true,
				pools = [ArtifactPool.Common]
			},
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/RerollArtifact.png")).Sprite,
			Name = AnyLocalizations.Bind(["artifact", "name"]).Localize,
			Description = AnyLocalizations.Bind(["artifact", "description"]).Localize
		});

		_ = new ArtifactRerollManager();
		_ = new CardRerollManager();
		_ = new RunStartManager();
		_ = new RerollChargeManager();

		helper.ModRegistry.AwaitApi<IModSettingsApi>(
			"Nickel.ModSettings",
			api => api.RegisterModSettings(api.MakeList([
				api.MakeProfileSelector(
					() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
					Settings.ProfileBased
				),
				api.MakeNumericStepper(
					() => Localizations.Localize(["modSettings", "initialRerolls"]),
					() => Settings.ProfileBased.Current.InitialRerolls,
					value => Settings.ProfileBased.Current.InitialRerolls = value,
					minValue: 0
				),
				api.MakeNumericStepper(
					() => Localizations.Localize(["modSettings", "rerollsAfterZone"]),
					() => Settings.ProfileBased.Current.RerollsAfterZone,
					value => Settings.ProfileBased.Current.RerollsAfterZone = value,
					minValue: 0
				),
				api.MakeCheckbox(
					() => Localizations.Localize(["modSettings", "shopRerolls"]),
					() => Settings.ProfileBased.Current.ShopRerolls,
					(_, _, value) => Settings.ProfileBased.Current.ShopRerolls = value
				)
			]).SubscribeToOnMenuClose(_ =>
			{
				helper.Storage.SaveJson(this.SettingsFile, this.Settings);
			}))
		);
	}
}
