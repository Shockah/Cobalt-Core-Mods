using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Johnson;

public sealed class ModEntry : SimpleMod
{
	internal const CardBrowse.Source UpgradableCardsInHandBrowseSource = (CardBrowse.Source)2137301;
	internal const CardBrowse.Source UpgradableCardsAnywhereBrowseSource = (CardBrowse.Source)2137302;
	internal const CardBrowse.Source TemporarilyUpgradedCardsBrowseSource = (CardBrowse.Source)2137303;
	internal const CardBrowse.Source UpgradableCardsAnywhereToTypeABrowseSource = (CardBrowse.Source)2137304;
	internal const CardBrowse.Source UpgradableCardsAnywhereToTypeBBrowseSource = (CardBrowse.Source)2137305;
	internal const CardBrowse.Source DiscountCardAnywhereBrowseSource = (CardBrowse.Source)2137306;

	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IJohnsonApi Api = new ApiImplementation();

	internal Harmony Harmony { get; }
	internal IKokoroApi KokoroApi { get; }
	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	internal IDeckEntry JohnsonDeck { get; }
	internal IStatusEntry CrunchTimeStatus { get; }

	internal ISpriteEntry TemporaryUpgradeIcon { get; }
	internal ISpriteEntry StrengthenIcon { get; }
	internal ISpriteEntry StrengthenHandIcon { get; }
	internal ISpriteEntry DiscountHandIcon { get; }

	internal static IReadOnlyList<Type> StarterCardTypes { get; } = [
		typeof(KickstartCard),
		typeof(LayoutCard),
	];

	internal static IReadOnlyList<Type> CommonCardTypes { get; } = [
		typeof(BuyLowCard),
		typeof(CaffeineBuzzCard),
		typeof(InvestmentCard),
		typeof(ProfitMarginCard),
		typeof(RevampCard),
		typeof(StrategizeCard),
		typeof(SupplimentCard),
	];

	internal static IReadOnlyList<Type> UncommonCardTypes { get; } = [
		typeof(ComboAttackCard),
		typeof(MergerCard),
		typeof(NumberCruncherCard),
		typeof(OutsourceCard),
		typeof(OvertimeCard),
		typeof(PromoteCard),
		typeof(TheWorksCard),
	];

	internal static IReadOnlyList<Type> RareCardTypes { get; } = [
		typeof(CapitalGainCard),
		typeof(CrunchTimeCard),
		typeof(DownsizeCard),
		typeof(MintCard),
		typeof(Quarter1Card),
	];

	internal static IReadOnlyList<Type> SpecialCardTypes { get; } = [
		typeof(BrainstormCard),
		typeof(BulletPointCard),
		typeof(BurnOutCard),
		typeof(DeadlineCard),
		typeof(LeverageCard),
		typeof(Quarter2Card),
		typeof(Quarter3Card),
		typeof(SellHighCard),
		typeof(SlideTransitionCard),
	];

	internal static IEnumerable<Type> AllCardTypes
		=> StarterCardTypes
			.Concat(CommonCardTypes)
			.Concat(UncommonCardTypes)
			.Concat(RareCardTypes)
			.Concat(SpecialCardTypes);

	internal static IReadOnlyList<Type> CommonArtifacts { get; } = [
		typeof(BriefcaseArtifact),
		typeof(CandyArtifact),
		typeof(CouponArtifact),
		typeof(EspressoShotArtifact),
		typeof(JumpTheCurveArtifact),
	];

	internal static IReadOnlyList<Type> BossArtifacts { get; } = [
		typeof(FrugalityArtifact),
		typeof(RAndDArtifact),
	];

	internal static IEnumerable<Type> AllArtifactTypes
		=> CommonArtifacts.Concat(BossArtifacts);

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

		_ = new CrunchTimeManager();
		_ = new StrengthenManager();
		_ = new TemporaryUpgradeManager();

		DynamicWidthCardAction.ApplyPatches(Harmony, logger);
		ASpecificCardOffering.ApplyPatches(Harmony, logger);
		CustomCardBrowse.ApplyPatches(Harmony, logger);
		CustomTTGlossary.ApplyPatches(Harmony);
		InPlaceCardUpgrade.ApplyPatches(Harmony, logger);

		CustomCardBrowse.RegisterCustomCardSource(
			UpgradableCardsInHandBrowseSource,
			new CustomCardBrowse.CustomCardSource(
				(_, _, _) => Loc.T("cardBrowse.title.upgrade"),
				(_, combat) => (combat?.hand ?? []).Where(c => c.IsUpgradable()).ToList()
			)
		);
		CustomCardBrowse.RegisterCustomCardSource(
			UpgradableCardsAnywhereBrowseSource,
			new CustomCardBrowse.CustomCardSource(
				(_, _, _) => Loc.T("cardBrowse.title.upgrade"),
				(state, combat) => state.deck.Concat(combat?.discard ?? []).Concat(combat?.hand ?? []).Where(c => c.IsUpgradable()).ToList()
			)
		);
		CustomCardBrowse.RegisterCustomCardSource(
			TemporarilyUpgradedCardsBrowseSource,
			new CustomCardBrowse.CustomCardSource(
				(_, _, _) => Loc.T("cardBrowse.title.upgrade"),
				(state, combat) => state.GetAllCards().Where(c => c.upgrade != Upgrade.None && c.IsTemporarilyUpgraded()).ToList()
			)
		);
		CustomCardBrowse.RegisterCustomCardSource(
			UpgradableCardsAnywhereToTypeABrowseSource,
			new CustomCardBrowse.CustomCardSource(
				(_, _, _) => Localizations.Localize(["browseSource", nameof(UpgradableCardsAnywhereToTypeABrowseSource)]),
				(state, combat) => state.deck.Concat(combat?.discard ?? []).Concat(combat?.hand ?? []).Where(c => c.IsUpgradable()).ToList()
			)
		);
		CustomCardBrowse.RegisterCustomCardSource(
			UpgradableCardsAnywhereToTypeBBrowseSource,
			new CustomCardBrowse.CustomCardSource(
				(_, _, _) => Localizations.Localize(["browseSource", nameof(UpgradableCardsAnywhereToTypeBBrowseSource)]),
				(state, combat) => state.deck.Concat(combat?.discard ?? []).Concat(combat?.hand ?? []).Where(c => c.IsUpgradable()).ToList()
			)
		);
		CustomCardBrowse.RegisterCustomCardSource(
			DiscountCardAnywhereBrowseSource,
			new CustomCardBrowse.CustomCardSource(
				(_, _, _) => Localizations.Localize(["browseSource", nameof(DiscountCardAnywhereBrowseSource)]),
				(state, combat) => state.deck.Concat(combat?.discard ?? []).Concat(combat?.hand ?? []).ToList()
			)
		);

		CrunchTimeStatus = helper.Content.Statuses.RegisterStatus("CrunchTime", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/CrunchTime.png")).Sprite,
				color = new("F7883E"),
				isGood = true
			},
			Name = this.AnyLocalizations.Bind(["status", "CrunchTime", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "CrunchTime", "description"]).Localize
		});

		JohnsonDeck = helper.Content.Decks.RegisterDeck("Johnson", new()
		{
			Definition = new() { color = new("2D3F61"), titleColor = Colors.white },
			DefaultCardArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Default.png")).Sprite,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});
		
		foreach (var cardType in AllCardTypes)
			AccessTools.DeclaredMethod(cardType, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
		foreach (var artifactType in AllArtifactTypes)
			AccessTools.DeclaredMethod(artifactType, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		helper.Content.Characters.RegisterCharacter("Johnson", new()
		{
			Deck = JohnsonDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			StarterCardTypes = StarterCardTypes,
			NeutralAnimation = new()
			{
				Deck = JohnsonDeck.Deck,
				LoopTag = "neutral",
				Frames = Enumerable.Range(0, 4)
					.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Neutral/{i}.png")).Sprite)
					.ToList()
			},
			MiniAnimation = new()
			{
				Deck = JohnsonDeck.Deck,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			}
		});

		helper.Content.Characters.RegisterCharacterAnimation(new()
		{
			Deck = JohnsonDeck.Deck,
			LoopTag = "gameover",
			Frames = Enumerable.Range(0, 1)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/GameOver/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.RegisterCharacterAnimation(new()
		{
			Deck = JohnsonDeck.Deck,
			LoopTag = "squint",
			Frames = Enumerable.Range(0, 5)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Squint/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.RegisterCharacterAnimation(new()
		{
			Deck = JohnsonDeck.Deck,
			LoopTag = "fiddling",
			Frames = Enumerable.Range(0, 4)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Fiddling/{i}.png")).Sprite)
				.ToList()
		});

		TemporaryUpgradeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/TemporaryUpgrade.png"));
		StrengthenIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Strengthen.png"));
		StrengthenHandIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/StrengthenHand.png"));
		DiscountHandIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/DiscountHand.png"));
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
