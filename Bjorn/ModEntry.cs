using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System;
using HarmonyLib;
using Shockah.Kokoro;
using System.Linq;
using Shockah.Shared;

namespace Shockah.Bjorn;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly IKokoroApi.IV2 KokoroApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal IDeckEntry BjornDeck { get; }

	internal static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(DrawConclusionsCard),
		typeof(ElectronGunCard),
		typeof(FractalStructureCard),
		typeof(LorentzTransformCard),
		typeof(PrototypingCard),
		typeof(SafetyProtocolCard),
		typeof(SmartShieldDroneCard),
		typeof(TaserCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(AssimilateCard),
		typeof(EntangleCard),
		typeof(InsuranceCard),
		typeof(RelativityCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(HandheldDuplitronCard),
		typeof(LilHadronColliderCard),
	];

	internal static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			typeof(PrototypeCard),
			//typeof(BlochExeCard),
		];

	internal static readonly IReadOnlyList<Type> CommonArtifacts = [
	];

	internal static readonly IReadOnlyList<Type> BossArtifacts = [
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
			typeof(Analyze),
			typeof(Entanglement),
			typeof(Relativity),
			typeof(SmartShield),
			typeof(SmartShieldDrone),
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

		BjornDeck = helper.Content.Decks.RegisterDeck("Bjorn", new()
		{
			Definition = new() { color = new("23EEB6"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		helper.Content.Characters.V2.RegisterPlayableCharacter("Bjorn", new()
		{
			Deck = BjornDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			NeutralAnimation = new()
			{
				CharacterType = BjornDeck.UniqueName,
				LoopTag = "neutral",
				Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/Neutral")
					.GetSequentialFiles(i => $"{i}.png")
					.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
					.ToList()
			},
			MiniAnimation = new()
			{
				CharacterType = BjornDeck.UniqueName,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			},
			Starters = new()
			{
				cards = [
					new SafetyProtocolCard(),
					new ElectronGunCard(),
				]
			},
			//ExeCardType = typeof(BlochExeCard),
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