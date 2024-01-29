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

	internal static ModEntry Instance { get; private set; } = null!;

	internal Harmony Harmony { get; }
	internal IKokoroApi KokoroApi { get; }
	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	internal IDeckEntry JohnsonDeck { get; }

	internal ISpriteEntry TemporaryUpgradeIcon { get; }
	internal ISpriteEntry StrengthenIcon { get; }

	internal static IReadOnlyList<Type> StarterCardTypes { get; } = [
		typeof(LayoutCard),
		typeof(PromoteCard),
	];

	internal static IReadOnlyList<Type> CommonCardTypes { get; } = [
		typeof(BuyLowCard),
		typeof(CaffeineBuzzCard),
		typeof(InvestmentCard),
		typeof(ProfitMarginCard),
	];

	internal static IReadOnlyList<Type> UncommonCardTypes { get; } = [
		typeof(ComboAttackCard),
		typeof(NumberCruncherCard),
	];

	internal static IReadOnlyList<Type> RareCardTypes { get; } = [
		typeof(DownsizeCard),
		typeof(Quarter1Card),
	];

	internal static IReadOnlyList<Type> SpecialCardTypes { get; } = [
		typeof(BulletPointCard),
		typeof(BurnOutCard),
		typeof(DeadlineCard),
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
	];

	internal static IReadOnlyList<Type> BossArtifacts { get; } = [
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

		_ = new TemporaryUpgradeManager();
		_ = new StrengthenManager();

		ASpecificCardOffering.ApplyPatches(Harmony, logger);
		CustomCardBrowse.ApplyPatches(Harmony, logger);
		CustomTTGlossary.ApplyPatches(Harmony);

		CustomCardBrowse.RegisterCustomCardSource(
			UpgradableCardsInHandBrowseSource,
			new CustomCardBrowse.CustomCardSource(
				(_, _, _) => Loc.T("cardBrowse.title.upgrade"),
				(_, combat) => combat.hand.Where(c => c.IsUpgradable()).ToList()
			)
		);

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

		TemporaryUpgradeIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/TemporaryUpgrade.png"));
		StrengthenIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Strengthen.png"));
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
