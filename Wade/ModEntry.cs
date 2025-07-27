using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System;
using HarmonyLib;
using Shockah.Kokoro;
using System.Linq;
using Shockah.Shared;

namespace Shockah.Wade;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	// internal readonly HookManager<IDestinyApi.IHook> HookManager;
	internal readonly IKokoroApi.IV2 KokoroApi;
	// internal IDuoArtifactsApi? DuoArtifactsApi { get; private set; }
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal IDeckEntry WadeDeck { get; }

	private static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(AllOrNothingCard),
		typeof(CheapTrickCard),
		typeof(HedgeYourBetsCard),
		typeof(HotStreakCard),
		typeof(LuckyBreakCard),
		typeof(OddShotCard),
		typeof(RollTheBonesCard),
		typeof(TrendSettingCard),
		typeof(WeighTheOddsCard),
	];

	private static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(AllInCard),
		typeof(AnalyzeCard),
		typeof(DefyTheOddsCard),
		typeof(LuckOfTheDrawCard),
		typeof(RiskyManeuverCard),
		typeof(SeeingRedCard),
		typeof(UpTheAnteCard),
	];

	private static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(DoubleDownCard),
		typeof(DumbLuckCard),
		typeof(LuckyDriveCard),
		typeof(SelfReflectionCard),
		typeof(TwistFateCard),
	];

	private static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			// typeof(DestinyExeCard),
		];

	private static readonly IReadOnlyList<Type> CommonArtifacts = [
	];

	private static readonly IReadOnlyList<Type> BossArtifacts = [
	];

	private static readonly IReadOnlyList<Type> DuoArtifacts = [
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
			typeof(Odds),
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		// HookManager = new(package.Manifest.UniqueName);
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		WadeDeck = helper.Content.Decks.RegisterDeck("Wade", new()
		{
			Definition = new() { color = new("56DF6A"), titleColor = Colors.white },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize,
			ShineColorOverride = args => DB.decks[args.Card.GetMeta().deck].color.normalize().gain(0.5),
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		helper.Content.Characters.V2.RegisterPlayableCharacter("Wade", new()
		{
			Deck = WadeDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			NeutralAnimation = new()
			{
				CharacterType = WadeDeck.UniqueName,
				LoopTag = "neutral",
				Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/Neutral")
					.GetSequentialFiles(i => $"{i}.png")
					.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
					.ToList()
			},
			MiniAnimation = new()
			{
				CharacterType = WadeDeck.UniqueName,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			},
			Starters = new()
			{
				cards = [
					new TrendSettingCard(),
					new OddShotCard(),
				]
			},
			// ExeCardType = typeof(DestinyExeCard),
		});
		
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = WadeDeck.UniqueName,
			LoopTag = "gameover",
			Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/GameOver")
				.GetSequentialFiles(i => $"{i}.png")
				.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = WadeDeck.UniqueName,
			LoopTag = "squint",
			Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/Squint")
				.GetSequentialFiles(i => $"{i}.png")
				.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
				.ToList()
		});
		
		// helper.ModRegistry.AwaitApi<IMoreDifficultiesApi>(
		// 	"TheJazMaster.MoreDifficulties",
		// 	new SemanticVersion(1, 3, 0),
		// 	api => api.RegisterAltStarters(
		// 		deck: WadeDeck.Deck,
		// 		starterDeck: new StarterDeck
		// 		{
		// 			cards = [
		// 				new PowerWordCard(),
		// 				new MeditateCard()
		// 			]
		// 		}
		// 	)
		// );
		//
		// helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		// {
		// 	if (phase != ModLoadPhase.AfterDbInit)
		// 		return;
		// 	if (helper.ModRegistry.GetApi<IDuoArtifactsApi>("Shockah.DuoArtifacts") is not { } duoArtifactsApi)
		// 		return;
		//
		// 	DuoArtifactsApi = duoArtifactsApi;
		// 	foreach (var type in DuoArtifacts)
		// 		AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
		// };
	}

	// public override object GetApi(IModManifest requestingMod)
	// 	=> new ApiImplementation();

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