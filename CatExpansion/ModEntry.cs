using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Essentials;

namespace Shockah.CatExpansion;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly IEssentialsApi EssentialsApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	
	internal Settings Settings { get; private set; }

	private static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(BitShiftCard),
		typeof(DroneDualityCard),
		typeof(RecollectionCard),
		typeof(SafetyFirstCard),
		typeof(StaticShotCard),
		typeof(SwordAndShieldCard),
	];

	private static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(BackToBasicsCard),
		typeof(CallbackCard),
		typeof(TripleThreatCard),
	];

	private static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(PriorityQueueCard),
	];

	private static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
		];

	private static readonly IReadOnlyList<Type> CommonArtifacts = [
		typeof(PatchNotesArtifact),
		typeof(SmallWormholeArtifact),
	];

	private static readonly IReadOnlyList<Type> BossArtifacts = [
		typeof(HotReloadArtifact),
		typeof(PersonalDataArtifact),
	];

	private static readonly IEnumerable<Type> AllArtifactTypes
		= [
			.. CommonArtifacts,
			.. BossArtifacts,
		];

	private static readonly IEnumerable<Type> RegisterableTypes
		= [
			.. AllCardTypes,
			.. AllArtifactTypes,
			typeof(ExeOfferingDistribution),
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
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
	}

	internal static Rarity GetCardRarity(Type type)
	{
		if (RareCardTypes.Contains(type))
			return Rarity.rare;
		if (UncommonCardTypes.Contains(type))
			return Rarity.uncommon;
		return Rarity.common;
	}

	internal static ArtifactPool[] GetArtifactPools(Type type)
	{
		if (BossArtifacts.Contains(type))
			return [ArtifactPool.Boss];
		if (CommonArtifacts.Contains(type))
			return [ArtifactPool.Common];
		return [];
	}
}