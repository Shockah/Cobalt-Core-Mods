using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.EventsGalore;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal Harmony Harmony { get; }
	internal IKokoroApi KokoroApi { get; }

	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	internal static IReadOnlyList<Type> StatusTypes { get; } = [
		typeof(VolatileOverdriveStatus),
	];

	internal static IReadOnlyList<Type> EnemyTypes { get; } = [
		typeof(VolatileOverdriveEnemy),
	];

	internal static IReadOnlyList<Type> EventTypes { get; } = [
		typeof(AbyssalPowerEvent),
		typeof(CombatDataCalibrationEvent),
		typeof(ShipSwapEvent),
	];

	internal static IEnumerable<Type> RegisterableTypes { get; } = StatusTypes.Concat(EnemyTypes).Concat(EventTypes);

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

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		helper.Events.OnLoadStringsForLocale += (_, e) =>
		{
			foreach (var type in RegisterableTypes)
				AccessTools.DeclaredMethod(type, nameof(IRegisterable.OnLoadStringsForLocale))?.Invoke(null, [package, helper, e]);
		};
	}
}
