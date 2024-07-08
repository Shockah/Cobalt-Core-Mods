using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Natasha;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly Harmony Harmony;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal readonly IDeckEntry NatashaDeck;

	internal static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(DenialOfServiceCard),
		typeof(RestartEnginesCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonCardTypes = [
		//typeof(ChangePerspectiveCard),
		//typeof(DelveDeepCard),
		//typeof(MindBlastCard),
		//typeof(MindPurgeCard),
		//typeof(OutburstCard),
		//typeof(OverstimulationCard),
		//typeof(RealityBendingCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		//typeof(CalmCard),
		//typeof(IntrusiveThoughtCard),
		//typeof(MindMapCard),
		//typeof(EmotionalDamageCard),
		//typeof(SplitPersonalityCard),
	];

	internal static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			//typeof(BlochExeCard)
		];

	internal static readonly IReadOnlyList<Type> CommonArtifacts = [
		//typeof(ComposureArtifact),
		//typeof(LastingInsightArtifact),
		//typeof(LongTermMemoryArtifact),
		//typeof(MuscleMemoryArtifact),
		//typeof(UnlockedPotentialArtifact),
		//typeof(VainMemoriesArtifact),
	];

	internal static readonly IReadOnlyList<Type> BossArtifacts = [
		//typeof(AuraMasteryArtifact),
		//typeof(FutureVisionArtifact),
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
			.. AllCardTypes,
			.. AllArtifactTypes,
			.. DuoArtifacts,
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		NatashaDeck = helper.Content.Decks.RegisterDeck("Natasha", new()
		{
			Definition = new() { color = new("E1FFCF"), titleColor = Colors.black },
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
				Frames = Enumerable.Range(0, 3)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Neutral/{i}.png")).Sprite)
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
					new DenialOfServiceCard(),
					new RestartEnginesCard(),
				]
			},
			//ExeCardType = typeof(BlochExeCard)
		});

		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = NatashaDeck.UniqueName,
			LoopTag = "gameover",
			Frames = Enumerable.Range(0, 1)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/GameOver/{i}.png")).Sprite)
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
