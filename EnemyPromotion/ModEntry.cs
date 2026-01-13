using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.EnemyPromotion;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal IHarmony Harmony { get; }
	internal IModSettingsApi? ModSettingsApi { get; private set; }
	
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	internal readonly MultiPool ArgsPool = new();
	internal readonly IEnemyPromotionApi Api = new ApiImplementation();
	
	internal Settings Settings { get; private set; }
	internal readonly Dictionary<string, Func<IEnemyPromotionApi.IPromotedEnemyHandlerArgs<AI>, AI>> PromotionHandlers = [];
	
	private static IReadOnlyList<Type> RegisterableTypes { get; } = [
		typeof(EnemyPromotion),
		
		// zone 1
		typeof(SimpleMissilerPromotion),
		typeof(MediumFighterPromotion),
		typeof(HeavyFighterPromotion),
		typeof(DroneDropperZ1Promotion),
	];
	
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
		
		helper.ModRegistry.AwaitApi<IModSettingsApi>("Nickel.ModSettings", api => ModSettingsApi = api);

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation();
}