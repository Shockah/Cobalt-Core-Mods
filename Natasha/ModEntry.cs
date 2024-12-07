using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Common;
using Shockah.Bloch;
using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Natasha;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly IKokoroApi.IV2 KokoroApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal readonly IDeckEntry NatashaDeck;
	internal readonly ISpriteEntry AddCardAIcon;
	internal readonly ISpriteEntry AddCardBIcon;

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
		typeof(BruteForceCard),
		typeof(HijackEnginesCard),
		typeof(ManInTheMiddleCard),
		typeof(OnionRoutingCard),
		typeof(ReprogramCard),
		typeof(TypoCard),
		typeof(VoltageTuningCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(AccessViolationCard),
		typeof(BotnetCard),
		typeof(PortScanningCard),
		typeof(RemoteExecutionCard),
		typeof(ZeroDayExploitCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonSpecialCardTypes = [
		typeof(DeprogramCard),
	];

	internal static readonly IReadOnlyList<Type> RareRemovedCardTypes = [
		typeof(RebootCard)
	];

	internal static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			.. UncommonSpecialCardTypes,
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
		typeof(GeneticAlgorithmArtifact),
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
			typeof(OneLiners),
			typeof(Reprogram),
			typeof(NegativeBoost),
			.. AllCardTypes,
			.. AllArtifactTypes,
			.. DuoArtifacts,
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

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

		AddCardAIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/AddCardA.png"));
		AddCardBIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/AddCardB.png"));

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
		if (RareRemovedCardTypes.Contains(type))
			return Rarity.rare;
		if (UncommonCardTypes.Contains(type))
			return Rarity.uncommon;
		if (UncommonSpecialCardTypes.Contains(type))
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
