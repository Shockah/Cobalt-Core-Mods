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

public sealed partial class ModEntry : IModManifest, IApiProviderManifest, ISpriteManifest, IDeckManifest, IStatusManifest, IAnimationManifest, IArtifactManifest, ICardManifest, ICharacterManifest
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal ApiImplementation Api { get; private set; } = new();
	private Harmony Harmony { get; set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal FrogproofManager FrogproofManager { get; private set; } = new();

	internal ExternalSprite SogginsDeckBorder { get; private set; } = ExternalSprite.GetRaw((int)StableSpr.cardShared_border_soggins);
	internal ExternalDeck SogginsDeck { get; private set; } = null!;
	internal ExternalCharacter SogginsCharacter { get; private set; } = null!;

	internal ExternalSprite SogginsMini { get; private set; } = null!;
	internal ExternalSprite SmugArtifactSprite { get; private set; } = null!;
	internal ExternalSprite SmugStatusSprite { get; private set; } = null!;

	internal ExternalStatus SmugStatus { get; private set; } = null!;

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
	};
	internal static readonly Type[] UncommonCards = new Type[]
	{
		typeof(HarnessingSmugnessCard),
	};

	internal static IEnumerable<Type> AllCards
		=> ApologyCards.Concat(CommonCards).Concat(UncommonCards);

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony = new(Name);
		SmugStatusManager.ApplyPatches(Harmony);
	}

	public object? GetApi(IManifest requestingMod)
		=> new ApiImplementation();

	public void LoadManifest(ISpriteRegistry registry)
	{
		SogginsMini = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Character.Soggins.Mini",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "SogginsMini.png"))
		);
		SmugArtifactSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Artifact.Smug",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "SmugArtifact.png"))
		);
		SmugStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.Smug",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "SmugStatus.png"))
		);
	}

	public void LoadManifest(IDeckRegistry registry)
	{
		SogginsDeck = new(
			globalName: $"{GetType().Namespace}.Deck.Soggins",
			deckColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF6A9C59)),
			titleColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF000000)),
			cardArtDefault: ExternalSprite.GetRaw((int)StableSpr.cards_colorless),
			borderSprite: ExternalSprite.GetRaw((int)StableSpr.cardShared_border_soggins),
			bordersOverSprite: null
		);
		registry.RegisterDeck(SogginsDeck);
	}

	public void LoadManifest(IStatusRegistry registry)
	{
		SmugStatus = new(
			$"{typeof(ModEntry).Namespace}.Status.Smug",
			isGood: false,
			mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFFBB00BB)),
			borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFFBB00BB)),
			SmugStatusSprite,
			affectedByTimestop: false
		);
		SmugStatus.AddLocalisation(I18n.SmugStatusName, I18n.SmugStatusDescription);
		registry.RegisterStatus(SmugStatus);
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
		ExternalArtifact smugnessArtifact = new(
			globalName: $"{GetType().Namespace}.Artifact.Smugness",
			artifactType: typeof(SmugArtifact),
			sprite: SmugArtifactSprite,
			ownerDeck: SogginsDeck
		);
		smugnessArtifact.AddLocalisation(I18n.SmugArtifactName.ToUpper(), I18n.SmugArtifactDescription);
		registry.RegisterArtifact(smugnessArtifact);
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
