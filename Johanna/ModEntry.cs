using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;
using Shockah.Shared;

namespace Shockah.Johanna;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal IKokoroApi.IV2 KokoroApi { get; }
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal readonly ISpriteEntry CommonCardFrame;
	internal readonly ISpriteEntry UncommonCardFrame;
	internal readonly ISpriteEntry RareCardFrame;
	internal readonly IDeckEntry JohannaDeck;

	private static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(ClusterRocketCard),
		typeof(CustomPayloadCard),
		typeof(EngineStallCard),
		typeof(HEClusterCard),
		typeof(SeekingClusterCard),
		typeof(ShiftClusterCard),
	];

	private static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(MissileSpamCard),
		typeof(OmnishiftCard),
		typeof(ReplicatorCard),
	];

	private static readonly IReadOnlyList<Type> RareCardTypes = [
	];

	private static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			// typeof(WadeExeCard),
		];

	private static readonly IReadOnlyList<Type> CommonArtifacts = [
	];

	private static readonly IReadOnlyList<Type> BossArtifacts = [
	];

	// private static readonly IReadOnlyList<Type> DuoArtifacts = [
	// ];

	private static readonly IEnumerable<Type> AllArtifactTypes
		= [
			.. CommonArtifacts,
			.. BossArtifacts,
		];

	private static readonly IEnumerable<Type> RegisterableTypes
		= [
			.. AllCardTypes,
			.. AllArtifactTypes,
			typeof(MissileCluster),
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

		AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(AnyLocalizations)
		);

		CommonCardFrame = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png"));
		UncommonCardFrame = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrameUncommon.png"));
		RareCardFrame = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrameRare.png"));
		JohannaDeck = helper.Content.Decks.RegisterDeck("Wade", new()
		{
			Definition = new() { color = new("56DF6A"), titleColor = Colors.white },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = CommonCardFrame.Sprite,
			Name = AnyLocalizations.Bind(["character", "name"]).Localize,
			ShineColorOverride = _ => Colors.black.fadeAlpha(0),
			CardFrameOverride = args => GetCardRarity(args.Card.GetType()) switch
			{
				Rarity.rare => RareCardFrame.Sprite,
				Rarity.uncommon => UncommonCardFrame.Sprite,
				_ => CommonCardFrame.Sprite,
			}
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
		
		helper.Content.Characters.V2.RegisterPlayableCharacter("Wade", new()
		{
			Deck = JohannaDeck.Deck,
			Description = AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			NeutralAnimation = new()
			{
				CharacterType = JohannaDeck.UniqueName,
				LoopTag = "neutral",
				Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/Neutral")
					.GetSequentialFiles(i => $"{i}.png")
					.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
					.ToList()
			},
			MiniAnimation = new()
			{
				CharacterType = JohannaDeck.UniqueName,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			},
			Starters = new()
			{
				cards = [
					new ShiftClusterCard(),
				],
			},
			// ExeCardType = typeof(WadeExeCard),
		});
		
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = JohannaDeck.UniqueName,
			LoopTag = "gameover",
			Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/GameOver")
				.GetSequentialFiles(i => $"{i}.png")
				.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
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
}