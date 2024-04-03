using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal Harmony Harmony { get; }
	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	internal IDeckEntry DynaDeck { get; }

	internal static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(BangCard),
		typeof(KaboomCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(SmokeBombCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(MegatonBlastCard),
	];

	internal static IEnumerable<Type> AllCardTypes
		=> CommonCardTypes
			.Concat(UncommonCardTypes)
			.Concat(RareCardTypes);

	internal static readonly IReadOnlyList<Type> CommonArtifacts = [
	];

	internal static readonly IReadOnlyList<Type> BossArtifacts = [
	];

	internal static readonly IEnumerable<Type> AllArtifactTypes
		= CommonArtifacts.Concat(BossArtifacts);

	internal static readonly IEnumerable<Type> RegisterableTypes
		= AllCardTypes.Concat(AllArtifactTypes);

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

		_ = new BlastwaveManager();

		DynaDeck = helper.Content.Decks.RegisterDeck("Dyna", new()
		{
			Definition = new() { color = new("EC592B"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = StableSpr.cardShared_border_colorless,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});

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
}
