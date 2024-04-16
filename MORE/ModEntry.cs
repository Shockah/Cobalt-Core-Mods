using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;

namespace Shockah.MORE;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal Harmony Harmony { get; }
	internal IKokoroApi KokoroApi { get; }

	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	internal static IReadOnlyList<Type> StatusTypes { get; } = [
		typeof(ActionReactionStatus),
		typeof(BombEnemy.SelfDestructTimerStatus),
		typeof(VolatileOverdriveStatus),
	];

	internal static IReadOnlyList<Type> EnemyTypes { get; } = [
		typeof(ActionReactionEnemy),
		typeof(BombEnemy),
		typeof(VolatileOverdriveEnemy),
	];

	internal static IReadOnlyList<Type> EventTypes { get; } = [
		typeof(AbyssalPowerEvent),
		typeof(CombatDataCalibrationEvent),
		typeof(ShipSwapEvent),
	];

	internal static IEnumerable<Type> RegisterableTypes { get; }
		= [
			..StatusTypes,
			..EnemyTypes,
			typeof(EphemeralUpgrades),
			typeof(ReleaseUpgrades),
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

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
		CustomTTGlossary.ApplyPatches(Harmony);

		helper.Events.OnLoadStringsForLocale += (_, e) =>
		{
			foreach (var type in RegisterableTypes)
				AccessTools.DeclaredMethod(type, nameof(IRegisterable.OnLoadStringsForLocale))?.Invoke(null, [package, helper, e]);
		};
	}

	public override object? GetApi(IModManifest requestingMod)
		=> new ApiImplementation();
}
