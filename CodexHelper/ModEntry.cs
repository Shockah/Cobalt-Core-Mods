using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.CodexHelper;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	internal readonly ICodexHelperApi Api;
	
	internal Settings Settings { get; }
	
	private static readonly IEnumerable<Type> RegisterableTypes = [
		typeof(ArtifactCodexProgress),
		typeof(CardCodexProgress),
	];
	
	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		Api = new ApiImplementation();
		
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
		
		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			if (helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings") is not { } api)
				return;

			api.RegisterModSettings(api.MakeList([
				api.MakeProfileSelector(
					() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
					Settings.ProfileBased
				),
				.. RegisterableTypes
					.SelectMany(
						type => AccessTools.DeclaredMethod(type, nameof(IRegisterable.MakeSettings))?.Invoke(null, [package, api]) is IModSettingsApi.IModSetting setting
							? new List<IModSettingsApi.IModSetting> { setting }
							: []
					)
			]).SubscribeToOnMenuClose(_ =>
			{
				helper.Storage.SaveJson(helper.Storage.GetMainStorageFile("json"), Settings);
			}));
		};
		
		NewRunOptionsPatches.Apply(Harmony);
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation();
}
