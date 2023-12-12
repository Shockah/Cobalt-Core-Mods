using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.Soggins;

public sealed partial class ModEntry : IModManifest, IPrelaunchManifest, IApiProviderManifest, ISpriteManifest, IDeckManifest, IStatusManifest, IAnimationManifest, IArtifactManifest, ICardManifest, ICharacterManifest
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal ApiImplementation Api { get; private set; } = new();
	internal IKokoroApi KokoroApi { get; private set; } = null!;
	private Harmony Harmony { get; set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[] { new DependencyEntry<IModManifest>("Shockah.Kokoro", ignoreIfMissing: false) };

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal SmugStatusManager SmugStatusManager { get; private set; } = null!;
	internal FrogproofManager FrogproofManager { get; private set; } = null!;

	internal ExternalSprite SogginsDeckBorder { get; private set; } = ExternalSprite.GetRaw((int)StableSpr.cardShared_border_soggins);
	internal ExternalDeck SogginsDeck { get; private set; } = null!;
	internal ExternalCharacter SogginsCharacter { get; private set; } = null!;

	internal ExternalSprite SogginsMini { get; private set; } = null!;
	internal ExternalSprite SmugStatusSprite { get; private set; } = null!;
	internal ExternalSprite FrogproofSprite { get; private set; } = null!;
	internal ExternalSprite BotchesStatusSprite { get; private set; } = null!;
	internal ExternalSprite ExtraApologiesStatusSprite { get; private set; } = null!;
	internal ExternalSprite ConstantApologiesStatusSprite { get; private set; } = null!;

	internal ExternalStatus SmuggedStatus { get; private set; } = null!;
	internal ExternalStatus SmugStatus { get; private set; } = null!;
	internal ExternalStatus FrogproofingStatus { get; private set; } = null!;
	internal ExternalStatus BotchesStatus { get; private set; } = null!;
	internal ExternalStatus ExtraApologiesStatus { get; private set; } = null!;
	internal ExternalStatus ConstantApologiesStatus { get; private set; } = null!;

	internal ExternalAnimation SogginsMadAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsMeekAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsNeutralAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsSmugAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsTubAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsMiniAnimation { get; private set; } = null!;

	internal static readonly Type[] ApologyCards = new Type[]
	{
		typeof(RandomPlaceholderApologyCard),
		typeof(AttackApologyCard),
		typeof(ShieldApologyCard),
		typeof(TempShieldApologyCard),
		typeof(EvadeApologyCard),
		typeof(DroneShiftApologyCard),
		typeof(MoveApologyCard),
		typeof(EnergyApologyCard),
		typeof(DrawApologyCard),
		typeof(AsteroidApologyCard),
		typeof(MissileApologyCard),
		typeof(MineApologyCard),
		typeof(HealApologyCard),
	};
	internal static readonly Type[] CommonCards = new Type[]
	{
		typeof(SmugnessControlCard),
		typeof(PressingButtonsCard),
		typeof(TakeCoverCard),
		typeof(ZenCard),
		typeof(MysteriousAmmoCard),
		typeof(RunningInCirclesCard),
		typeof(BetterSpaceMineCard),
		typeof(ThoughtsAndPrayersCard),
		typeof(StopItCard),
	};
	internal static readonly Type[] UncommonCards = new Type[]
	{
		typeof(HarnessingSmugnessCard),
		typeof(SoSorryCard),
		typeof(BetterThanYouCard),
		typeof(ImTryingCard),
	};
	internal static readonly Type[] RareCards = new Type[]
	{
		typeof(ExtraApologyCard),
		typeof(DoSomethingCard),
	};

	internal static IEnumerable<Type> AllCards
		=> ApologyCards.Concat(CommonCards).Concat(UncommonCards).Concat(RareCards);

	internal static readonly Type[] AllArtifacts = new Type[]
	{
		typeof(SmugArtifact),

		typeof(VideoWillArtifact),
		typeof(PiratedShipCadArtifact),
		typeof(HotTubArtifact),

		typeof(RepeatedMistakesArtifact),
	};

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));
		KokoroApi = contact.GetApi<IKokoroApi>("Shockah.Kokoro")!;

		SmugStatusManager = new();
		FrogproofManager = new();

		Harmony = new(Name);
		FrogproofManager.ApplyPatches(Harmony);
		SmugStatusManager.ApplyPatches(Harmony);
		CustomTTGlossary.ApplyPatches(Harmony);
	}

	public void FinalizePreperations(IPrelaunchContactPoint prelaunchManifest)
	{
		DBExtenderPatches.ApplyLatePatches(Harmony);
	}

	public object? GetApi(IManifest requestingMod)
		=> new ApiImplementation();

	public void LoadManifest(ISpriteRegistry registry)
	{
		SogginsMini = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Character.Soggins.Mini",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "SogginsMini.png"))
		);
		SmugStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.Smug",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "SmugStatus.png"))
		);
		FrogproofSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Icon.Frogproof",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "FrogproofIcon.png"))
		);
		BotchesStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.Botches",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "BotchesStatus.png"))
		);
		ExtraApologiesStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.ExtraApologies",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "ExtraApologiesStatus.png"))
		);
		ConstantApologiesStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.ConstantApologies",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "ConstantApologiesStatus.png"))
		);

		foreach (var cardType in AllArtifacts)
		{
			if (Activator.CreateInstance(cardType) is not IRegisterableArtifact card)
				continue;
			card.RegisterArt(registry);
		}
	}

	public void LoadManifest(IDeckRegistry registry)
	{
		SogginsDeck = new(
			globalName: $"{GetType().Namespace}.Deck.Soggins",
			deckColor: System.Drawing.Color.FromArgb(unchecked((int)0xFFB79CE5)), // 0xFF6A9C59
			titleColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF000000)),
			cardArtDefault: ExternalSprite.GetRaw((int)StableSpr.cards_colorless),
			borderSprite: ExternalSprite.GetRaw((int)StableSpr.cardShared_border_soggins),
			bordersOverSprite: null
		);
		registry.RegisterDeck(SogginsDeck);
	}

	public void LoadManifest(IStatusRegistry registry)
	{
		{
			SmuggedStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.Smugged",
				isGood: false,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF000000)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF000000)),
				SmugStatusSprite,
				affectedByTimestop: false
			);
			SmuggedStatus.AddLocalisation("<implementation details>", "<implementation details>");
			registry.RegisterStatus(SmuggedStatus);
		}
		{
			SmugStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.Smug",
				isGood: false,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF483C57)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF483C57)),
				SmugStatusSprite,
				affectedByTimestop: false
			);
			SmugStatus.AddLocalisation(I18n.SmugStatusName, I18n.SmugStatusDescription);
			registry.RegisterStatus(SmugStatus);
		}
		{
			FrogproofingStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.Frogproofing",
				isGood: true,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF483C57)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF483C57)),
				FrogproofSprite,
				affectedByTimestop: false
			);
			FrogproofingStatus.AddLocalisation(I18n.FrogproofingStatusName, I18n.FrogproofingStatusDescription);
			registry.RegisterStatus(FrogproofingStatus);
		}
		{
			BotchesStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.Botches",
				isGood: false,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF7E503C)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF7E503C)),
				BotchesStatusSprite,
				affectedByTimestop: false
			);
			BotchesStatus.AddLocalisation(I18n.BotchesStatusName, I18n.BotchesStatusDescription);
			registry.RegisterStatus(BotchesStatus);
		}
		{
			ExtraApologiesStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.ExtraApologies",
				isGood: false,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF2B5549)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF2B5549)),
				ExtraApologiesStatusSprite,
				affectedByTimestop: false
			);
			ExtraApologiesStatus.AddLocalisation(I18n.ExtraApologiesStatusName, I18n.ExtraApologiesStatusDescription);
			registry.RegisterStatus(ExtraApologiesStatus);
		}
		{
			ConstantApologiesStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.ConstantApologies",
				isGood: false,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF2B5549)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF2B5549)),
				ConstantApologiesStatusSprite,
				affectedByTimestop: false
			);
			ConstantApologiesStatus.AddLocalisation(I18n.ConstantApologiesStatusName, I18n.ConstantApologiesStatusDescription);
			registry.RegisterStatus(ConstantApologiesStatus);
		}

		// Biding Time: 639bff
		// Double Time: cc503d
	}

	public void LoadManifest(IAnimationRegistry registry)
	{
		{
			SogginsMadAnimation = new(
				$"{GetType().Namespace}.Animation.Soggins.Mad",
				deck: SogginsDeck,
				tag: "mad",
				intendedOverwrite: false,
				frames: new Spr[]
				{
					StableSpr.characters_soggins_soggins_mad_0,
					StableSpr.characters_soggins_soggins_mad_1,
					StableSpr.characters_soggins_soggins_mad_2,
				}.Select(s => ExternalSprite.GetRaw((int)s)).ToList()
			);
			registry.RegisterAnimation(SogginsMadAnimation);
		}
		{
			SogginsMeekAnimation = new(
				$"{GetType().Namespace}.Animation.Soggins.Meek",
				deck: SogginsDeck,
				tag: "meek",
				intendedOverwrite: false,
				frames: new Spr[]
				{
					StableSpr.characters_soggins_soggins_meek_0,
					StableSpr.characters_soggins_soggins_meek_1,
					StableSpr.characters_soggins_soggins_meek_2,
					StableSpr.characters_soggins_soggins_meek_3,
					StableSpr.characters_soggins_soggins_meek_4,
				}.Select(s => ExternalSprite.GetRaw((int)s)).ToList()
			);
			registry.RegisterAnimation(SogginsMeekAnimation);
		}
		{
			SogginsNeutralAnimation = new(
				$"{GetType().Namespace}.Animation.Soggins.Neutral",
				deck: SogginsDeck,
				tag: "neutral",
				intendedOverwrite: false,
				frames: new Spr[]
				{
					StableSpr.characters_soggins_soggins_neutral_0,
					StableSpr.characters_soggins_soggins_neutral_1,
					StableSpr.characters_soggins_soggins_neutral_2,
					StableSpr.characters_soggins_soggins_neutral_3,
					StableSpr.characters_soggins_soggins_neutral_4,
				}.Select(s => ExternalSprite.GetRaw((int)s)).ToList()
			);
			registry.RegisterAnimation(SogginsNeutralAnimation);
		}
		{
			SogginsSmugAnimation = new(
				$"{GetType().Namespace}.Animation.Soggins.Smug",
				deck: SogginsDeck,
				tag: "smug",
				intendedOverwrite: false,
				frames: new Spr[]
				{
					StableSpr.characters_soggins_soggins_smug_0,
					StableSpr.characters_soggins_soggins_smug_1,
					StableSpr.characters_soggins_soggins_smug_2,
					StableSpr.characters_soggins_soggins_smug_3,
					StableSpr.characters_soggins_soggins_smug_4,
				}.Select(s => ExternalSprite.GetRaw((int)s)).ToList()
			);
			registry.RegisterAnimation(SogginsSmugAnimation);
		}
		{
			SogginsTubAnimation = new(
				$"{GetType().Namespace}.Animation.Soggins.Tub",
				deck: SogginsDeck,
				tag: "tub",
				intendedOverwrite: false,
				frames: new Spr[]
				{
					StableSpr.characters_soggins_soggins_tub_0,
					StableSpr.characters_soggins_soggins_tub_1,
					StableSpr.characters_soggins_soggins_tub_2,
					StableSpr.characters_soggins_soggins_tub_3,
					StableSpr.characters_soggins_soggins_tub_4,
				}.Select(s => ExternalSprite.GetRaw((int)s)).ToList()
			);
			registry.RegisterAnimation(SogginsTubAnimation);
		}
		{
			SogginsMiniAnimation = new(
				$"{GetType().Namespace}.Animation.Soggins.Mini",
				deck: SogginsDeck,
				tag: "mini",
				intendedOverwrite: false,
				frames: new ExternalSprite[] { SogginsMini }
			);
			registry.RegisterAnimation(SogginsMiniAnimation);
		}
	}

	public void LoadManifest(IArtifactRegistry registry)
	{
		foreach (var cardType in AllArtifacts)
		{
			if (Activator.CreateInstance(cardType) is not IRegisterableArtifact card)
				continue;
			card.RegisterArtifact(registry);
			card.ApplyPatches(Harmony);
		}
	}

	public void LoadManifest(ICardRegistry registry)
	{
		foreach (var cardType in AllCards)
		{
			if (Activator.CreateInstance(cardType) is not IRegisterableCard card)
				continue;
			card.RegisterCard(registry);
			card.ApplyPatches(Harmony);
		}
	}

	public void LoadManifest(ICharacterRegistry registry)
	{
		SogginsCharacter = new ExternalCharacter(
			globalName: $"{GetType().Namespace}.Character.Soggins",
			deck: SogginsDeck,
			charPanelSpr: ExternalSprite.GetRaw((int)StableSpr.panels_char_soggins),
			starterDeck: new Type[] { typeof(SmugnessControlCard), typeof(PressingButtonsCard) },
			starterArtifacts: new Type[] { typeof(SmugArtifact) },
			neutralAnimation: SogginsNeutralAnimation,
			miniAnimation: SogginsMiniAnimation
		);
		SogginsCharacter.AddNameLocalisation(I18n.SogginsName);
		SogginsCharacter.AddDescLocalisation(I18n.SogginsDescription);
		registry.RegisterCharacter(SogginsCharacter);
	}
}
