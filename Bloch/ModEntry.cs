﻿using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly Harmony Harmony;
	internal readonly IKokoroApi KokoroApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal IDeckEntry BlochDeck { get; }

	internal static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(CalmCard),
		typeof(DistressCard),
		typeof(FeedbackCard),
		typeof(FocusCard),
		typeof(MaterializeCard),
		typeof(PainReactionCard),
		typeof(PsychicDamageCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(AttentionSpanCard),
		typeof(MindBlastCard),
		typeof(MindPurgeCard),
		typeof(SayThoughtsLoudCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(IntrusiveThoughtCard),
	];

	internal static readonly IReadOnlyList<Type> SpecialCardTypes = [
	];

	internal static readonly IEnumerable<Type> AllCardTypes
		= [
			..CommonCardTypes,
			..UncommonCardTypes,
			..RareCardTypes,
			..SpecialCardTypes,
		];

	internal static readonly IReadOnlyList<Type> CommonArtifacts = [
		typeof(VainMemoriesArtifact),
	];

	internal static readonly IReadOnlyList<Type> BossArtifacts = [
	];

	internal static readonly IReadOnlyList<Type> DuoArtifacts = [
	];

	internal static readonly IEnumerable<Type> AllArtifactTypes
		= [
			..CommonArtifacts,
			..BossArtifacts,
		];

	internal static readonly IEnumerable<Type> RegisterableTypes
		= [
			..AllCardTypes,
			..AllArtifactTypes,
			..DuoArtifacts,
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

		_ = new AuraManager();
		_ = new NegativeBoostManager();
		_ = new OnDiscardManager();
		_ = new OnTurnEndManager();
		_ = new OnHullDamageManager();
		_ = new OncePerTurnManager();

		BlochDeck = helper.Content.Decks.RegisterDeck("Bloch", new()
		{
			Definition = new() { color = new("C2FF60"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = StableSpr.cardShared_border_goat,
			//BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		helper.Content.Characters.RegisterCharacter("Bloch", new()
		{
			Deck = BlochDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			NeutralAnimation = new()
			{
				Deck = BlochDeck.Deck,
				LoopTag = "neutral",
				Frames = Enumerable.Range(0, 1)
					.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Neutral/{i}.png")).Sprite)
					.ToList()
			},
			MiniAnimation = new()
			{
				Deck = BlochDeck.Deck,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			},
			Starters = new()
			{
				cards = [
					new DistressCard(),
					new FocusCard(),
				]
			}
		});

		helper.Content.Characters.RegisterCharacterAnimation(new()
		{
			Deck = BlochDeck.Deck,
			LoopTag = "gameover",
			Frames = Enumerable.Range(0, 1)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/GameOver/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.RegisterCharacterAnimation(new()
		{
			Deck = BlochDeck.Deck,
			LoopTag = "squint",
			Frames = Enumerable.Range(0, 2)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Squint/{i}.png")).Sprite)
				.ToList()
		});
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