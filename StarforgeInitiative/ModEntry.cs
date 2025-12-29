using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.StarforgeInitiative;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal IHarmony Harmony { get; }
	internal IKokoroApi.IV2 KokoroApi { get; }

	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	// internal Settings Settings { get; private set; }

	internal static readonly List<Type> ShipManagerTypes = [
		typeof(AgniShip),
		typeof(BreadnaughtShip),
		typeof(KeplerShip),
		typeof(NemesisShip),
	];

	internal static readonly IEnumerable<Type> RegisterableTypes = [
		.. ShipManagerTypes,
	];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		// this.Settings = helper.Storage.LoadJson<Settings>(helper.Storage.GetMainStorageFile("json"));

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}

	// public override object GetApi(IModManifest requestingMod)
	// 	=> new ApiImplementation();
}
