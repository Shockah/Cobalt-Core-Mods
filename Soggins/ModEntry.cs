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

public sealed class ModEntry : IModManifest, ISpriteManifest, IDeckManifest, IAnimationManifest, IArtifactManifest, ICharacterManifest
{
	internal static ModEntry Instance { get; private set; } = null!;

	public string Name { get; init; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => Array.Empty<DependencyEntry>();

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }

	internal ExternalDeck SogginsDeck { get; private set; } = null!;
	internal ExternalCharacter SogginsCharacter { get; private set; } = null!;

	internal ExternalSprite SogginsMini { get; private set; } = null!;
	internal ExternalSprite SmugnessArtifactSprite { get; private set; } = null!;

	internal ExternalAnimation SogginsMadAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsMeekAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsNeutralAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsSmugAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsTubAnimation { get; private set; } = null!;
	internal ExternalAnimation SogginsMiniAnimation { get; private set; } = null!;

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.dll"));
		ReflectionExt.CurrentAssemblyLoadContext.LoadFromAssemblyPath(Path.Combine(ModRootFolder!.FullName, "Shrike.Harmony.dll"));

		Harmony harmony = new(Name);
		SmugnessArtifact.ApplyPatches(harmony);
	}

	public void LoadManifest(ISpriteRegistry registry)
	{
		SogginsMini = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Sprite.Soggins.Mini",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "SogginsMini.png"))
		);
		SmugnessArtifactSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Sprite.SmugnessArtifact",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "SmugnessArtifact.png"))
		);
	}

	public void LoadManifest(IDeckRegistry registry)
	{
		SogginsDeck = new(
			globalName: $"{GetType().Namespace}.Deck.Soggins",
			deckColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF6A9C59)),
			titleColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF6A9C59)),
			cardArtDefault: ExternalDeck.GetRaw((int)Deck.soggins).CardArtDefault,
			borderSprite: ExternalDeck.GetRaw((int)Deck.soggins).BorderSprite,
			bordersOverSprite: ExternalDeck.GetRaw((int)Deck.soggins).BordersOverSprite
		);
		registry.RegisterDeck(SogginsDeck);
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
			artifactType: typeof(SmugnessArtifact),
			sprite: SmugnessArtifactSprite,
			ownerDeck: SogginsDeck
		);
		smugnessArtifact.AddLocalisation(I18n.SmugnessArtifactName.ToUpper(), I18n.SmugnessArtifactDescription);
		registry.RegisterArtifact(smugnessArtifact);
	}

	public void LoadManifest(ICharacterRegistry registry)
	{
		SogginsCharacter = new ExternalCharacter(
			globalName: $"{GetType().Namespace}.Character.Soggins",
			deck: SogginsDeck,
			charPanelSpr: ExternalSprite.GetRaw((int)StableSpr.panels_char_soggins),
			starterDeck: Array.Empty<Type>(),
			starterArtifacts: new Type[] { typeof(SmugnessArtifact) },
			neutralAnimation: SogginsNeutralAnimation,
			miniAnimation: SogginsMiniAnimation
		);
		SogginsCharacter.AddNameLocalisation(I18n.SogginsName);
		SogginsCharacter.AddDescLocalisation(I18n.SogginsDescription);
		registry.RegisterCharacter(SogginsCharacter);
	}
}
