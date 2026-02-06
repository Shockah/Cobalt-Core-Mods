using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Common;
using Shockah.Kokoro;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Shockah.MORE;

namespace Shockah.Bloch;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly HookManager<IBlochHook> HookManager;
	internal readonly ApiImplementation Api;
	internal readonly IKokoroApi.IV2 KokoroApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> DialogueLocalizations;

	internal IDeckEntry BlochDeck { get; }

	internal static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(ApproachShiftCard),
		typeof(DistressCard),
		typeof(FeedbackCard),
		typeof(FocusCard),
		typeof(InsightCard),
		typeof(OptCard),
		typeof(PrismaticAuraCard),
		typeof(PsychicDamageCard),
		typeof(UnlockedPotentialCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(ChangePerspectiveCard),
		typeof(DelveDeepCard),
		typeof(MindBlastCard),
		typeof(MindPurgeCard),
		typeof(OnYourMindCard),
		typeof(OutburstCard),
		typeof(RealityBendingCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(DigressionCard),
		typeof(IntrusiveThoughtCard),
		typeof(MindMapCard),
		typeof(SplitPersonalityCard),
	];

	internal static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			typeof(BlochExeCard),
		];

	internal static readonly IReadOnlyList<Type> CommonArtifacts = [
		typeof(ComposureArtifact),
		typeof(LastingInsightArtifact),
		typeof(LongTermMemoryArtifact),
		typeof(MuscleMemoryArtifact),
		//typeof(VainMemoriesArtifact),
	];

	internal static readonly IReadOnlyList<Type> BossArtifacts = [
		typeof(AuraMasteryArtifact),
		typeof(FutureVisionArtifact),
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
			.. AllCardTypes,
			.. AllArtifactTypes,
			.. DuoArtifacts,
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		HookManager = new(package.Manifest.UniqueName);
		Api = new();
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/main-{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);
		this.DialogueLocalizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(new JsonLocalizationProvider(
				tokenExtractor: new SimpleLocalizationTokenExtractor(),
				localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/dialogue-{locale}.json").OpenRead()
			))
		);

		_ = new AuraManager();
		_ = new DiscardedVariableHintManager();
		_ = new DiscardSideManager();
		_ = new InfiniteCharacterAnimationManager();
		_ = new IntuitionManager();
		_ = new MindMapManager();
		_ = new ScryManager();
		_ = new SplitPersonalityManager();
		_ = new WavyDialogueManager();

		BlochDeck = helper.Content.Decks.RegisterDeck("Bloch", new()
		{
			Definition = new() { color = new("C2FF60"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		helper.Content.Characters.V2.RegisterPlayableCharacter("Bloch", new()
		{
			Deck = BlochDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			NeutralAnimation = new()
			{
				CharacterType = BlochDeck.UniqueName,
				LoopTag = "neutral",
				Frames = Enumerable.Range(0, 8)
					.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Neutral/{i}.png")).Sprite)
					.ToList()
			},
			MiniAnimation = new()
			{
				CharacterType = BlochDeck.UniqueName,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			},
			Starters = new()
			{
				cards = [
					new DistressCard(),
					new PsychicDamageCard(),
				]
			},
			ExeCardType = typeof(BlochExeCard)
		});

		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = BlochDeck.UniqueName,
			LoopTag = "gameover",
			Frames = Enumerable.Range(0, 1)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/GameOver/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = BlochDeck.UniqueName,
			LoopTag = "squint",
			Frames = Enumerable.Range(0, 7)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Squint/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = BlochDeck.UniqueName,
			LoopTag = "glerp",
			Frames = Enumerable.Range(0, 10)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Glerp/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = BlochDeck.UniqueName,
			LoopTag = "gloop",
			Frames = Enumerable.Range(0, 10)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Gloop/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = BlochDeck.UniqueName,
			LoopTag = "glorp",
			Frames = Enumerable.Range(0, 13)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Glorp/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = BlochDeck.UniqueName,
			LoopTag = "talking",
			Frames = Enumerable.Range(0, 4)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Talking/{i}.png")).Sprite)
				.ToList()
		});

		helper.ModRegistry.AwaitApi<IMoreDifficultiesApi>(
			"TheJazMaster.MoreDifficulties",
			new SemanticVersion(1, 3, 0),
			api => api.RegisterAltStarters(
				deck: BlochDeck.Deck,
				starterDeck: new StarterDeck
				{
					cards = [
						new FocusCard(),
						new FeedbackCard()
					]
				}
			)
		);

		helper.ModRegistry.AwaitApi<IDraculaApi>(
			"Shockah.Dracula",
			api =>
			{
				api.RegisterBloodTapOptionProvider(AuraManager.VeilingStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
					new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				]);
				api.RegisterBloodTapOptionProvider(AuraManager.FeedbackStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
					new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				]);
				api.RegisterBloodTapOptionProvider(AuraManager.InsightStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
					new AStatus { targetPlayer = true, status = AuraManager.IntensifyStatus.Status, statusAmount = 1 },
				]);
				api.RegisterBloodTapOptionProvider(IntuitionManager.IntuitionStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
				]);
				api.RegisterBloodTapOptionProvider(MindMapManager.MindMapStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
				]);
				api.RegisterBloodTapOptionProvider(SplitPersonalityManager.SplitPersonalityStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
				]);
			}
		);

		helper.ModRegistry.AwaitApi<IAppleArtifactApi>(
			"APurpleApple.GenericArtifacts",
			api => api.SetPaletteAction(
				BlochDeck.Deck,
				_ => new AStatus
				{
					targetPlayer = true,
					status = AuraManager.VeilingStatus.Status,
					statusAmount = 1,
				},
				new TTText(Localizations.Localize(["integration", "palette"]))
			)
		);
		
		helper.ModRegistry.AwaitApi<IMoreApi>(
			"Shockah.MORE",
			api => api.RegisterAltruisticArtifact(LongTermMemoryArtifact.Entry.UniqueName)
		);

		_ = new BasicDialogue();
		_ = new CombatDialogue();
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
