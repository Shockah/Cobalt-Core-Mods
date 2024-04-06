﻿using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly Harmony Harmony;
	internal readonly HookManager<IDynaHook> HookManager;
	internal readonly ApiImplementation Api;
	internal readonly IKokoroApi KokoroApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal IDeckEntry DynaDeck { get; }

	internal static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(BangCard),
		typeof(BurstChargeCard),
		typeof(ClearAPathCard),
		typeof(DemoChargeCard),
		typeof(FlashBurstCard),
		typeof(FluxChargeCard),
		typeof(IncomingCard),
		typeof(KaboomCard),
		typeof(SwiftChargeCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(BlitzkriegCard),
		typeof(BunkerCard),
		typeof(LightItUpCard),
		typeof(LockAndLoadCard),
		typeof(PerkUpCard),
		typeof(RemoteDetonatorCard),
		typeof(SmokeBombCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(BastionCard),
		typeof(ConcussionChargeCard),
		typeof(MegatonBlastCard),
		typeof(NitroCard),
		typeof(ShatterChargeCard),
	];

	internal static readonly IReadOnlyList<Type> SpecialCardTypes = [
		typeof(CustomChargeCard),
	];

	internal static IEnumerable<Type> AllCardTypes
		=> CommonCardTypes
			.Concat(UncommonCardTypes)
			.Concat(RareCardTypes)
			.Concat(SpecialCardTypes);

	internal static readonly IReadOnlyList<Type> CommonArtifacts = [
	];

	internal static readonly IReadOnlyList<Type> BossArtifacts = [
	];

	internal static readonly IEnumerable<Type> AllArtifactTypes
		= CommonArtifacts.Concat(BossArtifacts);

	internal static readonly IReadOnlyList<Type> ChargeTypes = [
		typeof(BurstCharge),
		typeof(ConcussionCharge),
		typeof(DemoCharge),
		typeof(FluxCharge),
		typeof(ShatterCharge),
		typeof(SwiftCharge),
	];

	internal static readonly IEnumerable<Type> RegisterableTypes
		= AllCardTypes.Concat(AllArtifactTypes).Concat(ChargeTypes);

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);
		HookManager = new();
		Api = new();
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		_ = new BlastwaveManager();
		_ = new ChargeManager();
		_ = new NitroManager();
		_ = new BastionManager();
		_ = new FluxPartModManager();
		CustomTTGlossary.ApplyPatches(Harmony);

		DynaDeck = helper.Content.Decks.RegisterDeck("Dyna", new()
		{
			Definition = new() { color = new("EC592B"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}

	public override object? GetApi(IModManifest requestingMod)
		=> new ApiImplementation();

	internal static Rarity GetCardRarity(Type type)
	{
		if (RareCardTypes.Contains(type))
			return Rarity.rare;
		if (UncommonCardTypes.Contains(type))
			return Rarity.uncommon;
		return Rarity.common;
	}
}