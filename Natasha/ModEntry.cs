﻿using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Common;
using Shockah.Bloch;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Natasha;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly HookManager<INatashaHook> HookManager;
	internal readonly IKokoroApi KokoroApi;
	internal readonly IBlochApi BlochApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal readonly IDeckEntry NatashaDeck;

	internal static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(BufferCard),
		typeof(BufferOverflowCard),
		typeof(ConcurrencyCard),
		typeof(DenialOfServiceCard),
		typeof(IfElseCard),
		typeof(PingCard),
		typeof(RemoveLimiterCard),
		typeof(SelfAlteringCodeCard),
		typeof(SpywareCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(AccessViolationCard),
		typeof(BruteForceCard),
		typeof(HijackEnginesCard),
		typeof(ManInTheMiddleCard),
		typeof(ReprogramCard),
		typeof(TypoCard),
		typeof(VoltageTuningCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(BotnetCard),
		typeof(PortScanningCard),
		typeof(RebootCard),
		typeof(RemoteExecutionCard),
		typeof(ZeroDayExploitCard),
	];

	internal static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			typeof(LimiterCard),
			typeof(NatashaExeCard)
		];

	internal static readonly IReadOnlyList<Type> CommonArtifacts = [
		typeof(DarkWebDataArtifact),
		typeof(ForkbombArtifact),
		typeof(KeyloggerArtifact),
		typeof(OperationAltairArtifact),
		typeof(TraceProtectionLinkArtifact),
	];

	internal static readonly IReadOnlyList<Type> BossArtifacts = [
		typeof(NetworkComputingArtifact),
		typeof(RamDiskArtifact),
	];

	internal static readonly IReadOnlyList<Type> DuoArtifacts = [
	];

	internal static readonly IEnumerable<Type> AllArtifactTypes
		= [
			.. CommonArtifacts,
			.. BossArtifacts,
		];

	internal static readonly IEnumerable<Type> RegisterableTypes
		= [
			typeof(Limited),
			typeof(TimesPlayed),
			typeof(Sequence),
			typeof(OneLiners),
			typeof(Reprogram),
			typeof(NegativeBoost),
			typeof(CardSelectFilters),
			.. AllCardTypes,
			.. AllArtifactTypes,
			.. DuoArtifacts,
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		HookManager = new();
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!;
		BlochApi = helper.ModRegistry.GetApi<IBlochApi>("Shockah.Bloch")!;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		NatashaDeck = helper.Content.Decks.RegisterDeck("Natasha", new()
		{
			Definition = new() { color = new("BBBBBB"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
		
		helper.Content.Characters.V2.RegisterPlayableCharacter("Natasha", new()
		{
			Deck = NatashaDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			NeutralAnimation = new()
			{
				CharacterType = NatashaDeck.UniqueName,
				LoopTag = "neutral",
				Frames = Enumerable.Range(0, 100)
					.Select(i => package.PackageRoot.GetRelativeFile($"assets/Character/Neutral/{i}.png"))
					.TakeWhile(f => f.Exists)
					.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
					.ToList()
			},
			MiniAnimation = new()
			{
				CharacterType = NatashaDeck.UniqueName,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			},
			Starters = new()
			{
				cards = [
					new SpywareCard(),
					new PingCard(),
				]
			},
			ExeCardType = typeof(NatashaExeCard)
		});

		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = NatashaDeck.UniqueName,
			LoopTag = "gameover",
			Frames = Enumerable.Range(0, 100)
				.Select(i => package.PackageRoot.GetRelativeFile($"assets/Character/GameOver/{i}.png"))
				.TakeWhile(f => f.Exists)
				.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = NatashaDeck.UniqueName,
			LoopTag = "squint",
			Frames = Enumerable.Range(0, 100)
				.Select(i => package.PackageRoot.GetRelativeFile($"assets/Character/Squint/{i}.png"))
				.TakeWhile(f => f.Exists)
				.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
				.ToList()
		});

		helper.ModRegistry.AwaitApi<IMoreDifficultiesApi>(
			"TheJazMaster.MoreDifficulties",
			new SemanticVersion(1, 3, 0),
			api => api.RegisterAltStarters(
				deck: NatashaDeck.Deck,
				starterDeck: new StarterDeck
				{
					cards = [
						new RemoveLimiterCard(),
						new BufferOverflowCard()
					]
				}
			)
		);

		helper.ModRegistry.AwaitApi<IDraculaApi>(
			"Shockah.Dracula",
			api =>
			{
				api.RegisterBloodTapOptionProvider(Reprogram.ReprogrammedStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
				]);
				api.RegisterBloodTapOptionProvider(Reprogram.DeprogrammedStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 2 },
				]);
			}
		);
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

	internal static ArtifactPool[] GetArtifactPools(Type type)
	{
		if (BossArtifacts.Contains(type))
			return [ArtifactPool.Boss];
		if (CommonArtifacts.Contains(type))
			return [ArtifactPool.Common];
		return [];
	}
}
