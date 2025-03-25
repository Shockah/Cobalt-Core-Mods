using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System;
using HarmonyLib;
using Shockah.Kokoro;
using System.Linq;
using Nickel.Common;
using Shockah.DuoArtifacts;
using Shockah.Shared;

namespace Shockah.Destiny;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly HookManager<IDestinyApi.IHook> HookManager;
	internal readonly IKokoroApi.IV2 KokoroApi;
	internal IDuoArtifactsApi? DuoArtifactsApi { get; private set; }
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal IDeckEntry DestinyDeck { get; }

	private static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(ComposureCard),
		typeof(CrashCard),
		typeof(FocusCard),
		typeof(ForcefieldCard),
		typeof(GleamCard),
		typeof(HoneCard),
		typeof(MeditateCard),
		typeof(PowerWordCard),
		typeof(ResearchCard),
	];

	private static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(BarrierCard),
		typeof(BraceCard),
		typeof(BulwarkCard),
		typeof(ExplosivoCard),
		typeof(OmniscienceCard),
		typeof(ReviseCard),
		typeof(StackCard),
	];

	private static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(DuplicateCard),
		typeof(GoForBrokeCard),
		typeof(ImmovableObjectCard),
		typeof(UnstableMagicCard),
		typeof(UnwaveringCard),
	];

	private static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			typeof(DestinyExeCard),
		];

	private static readonly IReadOnlyList<Type> CommonArtifacts = [
		typeof(ChainReactionArtifact),
		typeof(ShardBankArtifact),
		typeof(ShieldProficiencyArtifact),
		typeof(WellReadArtifact),
	];

	private static readonly IReadOnlyList<Type> BossArtifacts = [
		typeof(SanctuaryArtifact),
	];

	private static readonly IReadOnlyList<Type> DuoArtifacts = [
		typeof(DestinyCatArtifact),
		typeof(DestinyDizzyArtifact),
		typeof(DestinyDrakeArtifact),
		typeof(DestinyDynaArtifact),
		typeof(DestinyPeriArtifact),
		typeof(DestinyRiggsArtifact),
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
			typeof(Enchanted),
			typeof(Explosive),
			typeof(Imbue),
			typeof(MagicFind),
			typeof(NegativeMaxShard),
			typeof(PristineShield),
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		HookManager = new(package.Manifest.UniqueName);
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		DestinyDeck = helper.Content.Decks.RegisterDeck("Destiny", new()
		{
			Definition = new() { color = new("CB9077"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize,
			ShineColorOverride = args => DB.decks[args.Card.GetMeta().deck].color.normalize().gain(0.5),
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		helper.Content.Characters.V2.RegisterPlayableCharacter("Destiny", new()
		{
			Deck = DestinyDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			NeutralAnimation = new()
			{
				CharacterType = DestinyDeck.UniqueName,
				LoopTag = "neutral",
				Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/Neutral")
					.GetSequentialFiles(i => $"{i}.png")
					.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
					.ToList()
			},
			MiniAnimation = new()
			{
				CharacterType = DestinyDeck.UniqueName,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			},
			Starters = new()
			{
				cards = [
					new HoneCard(),
					new GleamCard(),
				]
			},
			ExeCardType = typeof(DestinyExeCard),
		});
		
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = DestinyDeck.UniqueName,
			LoopTag = "gameover",
			Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/GameOver")
				.GetSequentialFiles(i => $"{i}.png")
				.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = DestinyDeck.UniqueName,
			LoopTag = "squint",
			Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/Squint")
				.GetSequentialFiles(i => $"{i}.png")
				.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
				.ToList()
		});
		
		helper.ModRegistry.AwaitApi<IMoreDifficultiesApi>(
			"TheJazMaster.MoreDifficulties",
			new SemanticVersion(1, 3, 0),
			api => api.RegisterAltStarters(
				deck: DestinyDeck.Deck,
				starterDeck: new StarterDeck
				{
					cards = [
						new PowerWordCard(),
						new MeditateCard()
					]
				}
			)
		);
		
		helper.ModRegistry.AwaitApi<IDraculaApi>(
			"Shockah.Dracula",
			api => api.RegisterBloodTapOptionProvider(MagicFind.MagicFindStatus.Status, (_, _, status) => [
				new AHurt { targetPlayer = true, hurtAmount = 1 },
				new AStatus { targetPlayer = true, status = status, statusAmount = 4 },
			])
		);

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			if (helper.ModRegistry.GetApi<IDuoArtifactsApi>("Shockah.DuoArtifacts") is not { } duoArtifactsApi)
				return;

			DuoArtifactsApi = duoArtifactsApi;
			foreach (var type in DuoArtifacts)
				AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
		};
	}

	public override object GetApi(IModManifest requestingMod)
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