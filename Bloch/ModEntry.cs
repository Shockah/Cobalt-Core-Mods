using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Common;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Bloch;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly Harmony Harmony;
	internal readonly HookManager<IBlochHook> HookManager;
	internal readonly ApiImplementation Api;
	internal readonly IKokoroApi KokoroApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal IDeckEntry BlochDeck { get; }

	internal static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(CalmCard),
		typeof(DistressCard),
		typeof(FeedbackCard),
		typeof(FocusCard),
		typeof(InsightCard),
		typeof(MaterializeCard),
		typeof(OptCard),
		typeof(PainReactionCard),
		typeof(PsychicDamageCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(AttentionSpanCard),
		typeof(ChangePerspectiveCard),
		typeof(DelveDeepCard),
		typeof(MindBlastCard),
		typeof(MindPurgeCard),
		typeof(PrismaticAuraCard),
		typeof(SayThoughtsLoudCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(IntrusiveThoughtCard),
		typeof(MindMapCard),
		typeof(OutburstCard),
		typeof(OverstimulationCard),
		typeof(SplitPersonalityCard),
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
		typeof(ComposureArtifact),
		typeof(LastingInsightArtifact),
		typeof(LongTermMemoryArtifact),
		typeof(MuscleMemoryArtifact),
		typeof(UnlockedPotentialArtifact),
		typeof(VainMemoriesArtifact),
	];

	internal static readonly IReadOnlyList<Type> BossArtifacts = [
		typeof(AuraMasteryArtifact),
		typeof(FutureVisionArtifact),
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

		_ = new AuraManager();
		_ = new DrawEachTurnManager();
		_ = new NegativeBoostManager();
		_ = new OnDiscardManager();
		_ = new OnTurnEndManager();
		_ = new OnHullDamageManager();
		_ = new OncePerTurnManager();
		_ = new RetainManager();
		_ = new ScryManager();
		_ = new SplitPersonalityManager();

		BlochDeck = helper.Content.Decks.RegisterDeck("Bloch", new()
		{
			Definition = new() { color = new("C2FF60"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
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

		helper.ModRegistry.GetApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties", new SemanticVersion(1, 3, 0))?.RegisterAltStarters(
			deck: BlochDeck.Deck,
			starterDeck: new StarterDeck
			{
				cards = [
					new OptCard(),
					new PrismaticAuraCard()
				]
			}
		);

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;

			if (helper.ModRegistry.GetApi<IDraculaApi>("Shockah.Dracula") is { } draculaApi)
			{
				draculaApi.RegisterBloodTapOptionProvider(AuraManager.VeilingStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
					new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				]);
				draculaApi.RegisterBloodTapOptionProvider(AuraManager.FeedbackStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
					new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				]);
				draculaApi.RegisterBloodTapOptionProvider(AuraManager.InsightStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
					new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				]);
				draculaApi.RegisterBloodTapOptionProvider(DrawEachTurnManager.DrawEachTurnStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
				]);
				draculaApi.RegisterBloodTapOptionProvider(RetainManager.RetainStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
				]);
				draculaApi.RegisterBloodTapOptionProvider(SplitPersonalityManager.SplitPersonalityStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
				]);
			}
		};
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
