using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Legacy;
using Shockah.Kokoro;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.Soggins;

public sealed class ModEntry : CobaltCoreModding.Definitions.ModManifests.IModManifest, IApiProviderManifest, ISpriteManifest, IDeckManifest, IStatusManifest, IAnimationManifest, IArtifactManifest, ICardManifest, ICharacterManifest, INickelManifest
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal ApiImplementation Api { get; private set; } = null!;
	internal IKokoroApi.IV2 KokoroApi { get; private set; } = null!;
	internal IDuoArtifactsApi? DuoArtifactsApi { get; private set; }
	private Harmony Harmony { get; set; } = null!;

	public string Name { get; } = typeof(ModEntry).Namespace!;
	public IEnumerable<DependencyEntry> Dependencies => [
		new DependencyEntry<CobaltCoreModding.Definitions.ModManifests.IModManifest>("Shockah.Kokoro", ignoreIfMissing: false),
		new DependencyEntry<CobaltCoreModding.Definitions.ModManifests.IModManifest>("Shockah.DuoArtifacts", ignoreIfMissing: true)
	];

	public DirectoryInfo? GameRootFolder { get; set; }
	public DirectoryInfo? ModRootFolder { get; set; }
	public ILogger? Logger { get; set; }
	internal IModHelper Helper { get; private set; } = null!;
	internal IPluginPackage<Nickel.IModManifest> Package { get; private set; } = null!;

	internal SmugStatusManager SmugStatusManager { get; private set; } = null!;
	internal FrogproofManager FrogproofManager { get; private set; } = null!;
	internal NarrativeManager NarrativeManager { get; private set; } = null!;
	internal StatusRenderManager StatusRenderManager { get; private set; } = null!;
	internal StatusLogicManager StatusLogicManager { get; private set; } = null!;

	internal ExternalSprite SogginsDeckBorder { get; private set; } = null!;
	internal ExternalSprite ApologyDeckBorder { get; private set; } = null!;
	internal ExternalSprite SogginsCharacterBorder { get; private set; } = null!;
	internal ExternalDeck SogginsDeck { get; private set; } = null!;
	internal ExternalDeck ApologiesDeck { get; private set; } = null!;
	internal ExternalCharacter SogginsCharacter { get; private set; } = null!;
	internal ICardTraitEntry FrogproofTrait { get; private set; } = null!;

	internal ExternalSprite MiniPortraitSprite { get; private set; } = null!;
	internal Dictionary<int, List<ExternalSprite>> SmugPortraitSprites { get; } = [];
	internal List<ExternalSprite> OversmugPortraitSprites { get; } = [];
	internal List<ExternalSprite> SquintPortraitSprites { get; } = [];
	internal List<ExternalSprite> MadPortraitSprites { get; } = [];

	internal ExternalSprite SmugStatusSprite { get; private set; } = null!;
	internal ExternalSprite FrogproofSprite { get; private set; } = null!;
	internal ExternalSprite BotchesStatusSprite { get; private set; } = null!;
	internal ExternalSprite ExtraApologiesStatusSprite { get; private set; } = null!;
	internal ExternalSprite ConstantApologiesStatusSprite { get; private set; } = null!;
	internal ExternalSprite BidingTimeStatusSprite { get; private set; } = null!;
	internal ExternalSprite DoubleTimeStatusSprite { get; private set; } = null!;
	internal ExternalSprite DoublersLuckStatusSprite { get; private set; } = null!;

	internal ExternalStatus SmugStatus { get; private set; } = null!;
	internal ExternalStatus FrogproofingStatus { get; private set; } = null!;
	internal ExternalStatus BotchesStatus { get; private set; } = null!;
	internal ExternalStatus ExtraApologiesStatus { get; private set; } = null!;
	internal ExternalStatus ConstantApologiesStatus { get; private set; } = null!;
	internal ExternalStatus BidingTimeStatus { get; private set; } = null!;
	internal ExternalStatus DoubleTimeStatus { get; private set; } = null!;
	internal ExternalStatus DoublersLuckStatus { get; private set; } = null!;

	internal Dictionary<int, ExternalAnimation> SmugPortraitAnimations { get; private init; } = [];
	internal ExternalAnimation OversmugPortraitAnimation { get; private set; } = null!;
	internal ExternalAnimation NeutralPortraitAnimation { get; private set; } = null!;
	internal ExternalAnimation SquintPortraitAnimation { get; private set; } = null!;
	internal ExternalAnimation MadPortraitAnimation { get; private set; } = null!;
	internal ExternalAnimation GameOverPortraitAnimation { get; private set; } = null!;
	internal ExternalAnimation MiniPortraitAnimation { get; private set; } = null!;

	internal static readonly Type[] ApologyCards = [
		typeof(RandomPlaceholderApologyCard),
		typeof(DualApologyCard),
		typeof(AttackApologyCard),
		typeof(ShieldApologyCard),
		typeof(TempShieldApologyCard),
		typeof(EvadeApologyCard),
		typeof(DroneShiftApologyCard),
		typeof(MoveApologyCard),
		typeof(EnergyApologyCard),
		typeof(DrawApologyCard),
		typeof(RedrawApologyCard),
		typeof(AsteroidApologyCard),
		typeof(MissileApologyCard),
		typeof(MineApologyCard),
		typeof(HealApologyCard),
	];
	internal static readonly Type[] StarterCards = [
		typeof(SmugnessControlCard),
		typeof(PressingButtonsCard),
	];
	internal static readonly Type[] CommonCards = [
		typeof(TakeCoverCard),
		typeof(ZenCard),
		typeof(MysteriousAmmoCard),
		typeof(RunningInCirclesCard),
		typeof(BetterSpaceMineCard),
		typeof(ThoughtsAndPrayersCard),
		typeof(HumiliatingAttackCard),
	];
	internal static readonly Type[] UncommonCards = [
		typeof(HarnessingSmugnessCard),
		typeof(SoSorryCard),
		typeof(BetterThanYouCard),
		typeof(ImTryingCard),
		typeof(BlastFromThePastCard),
		typeof(BegForMercyCard),
		typeof(StopItCard),
	];
	internal static readonly Type[] RareCards = [
		typeof(ClonedSeekerCard),
		typeof(ClonedMissileMalwareCard),
		typeof(ExtraApologyCard),
		typeof(DoSomethingCard),
		typeof(ImAlwaysRightCard),
	];

	internal static IEnumerable<Type> AllCards
		=> ApologyCards.Concat(StarterCards).Concat(CommonCards).Concat(UncommonCards).Concat(RareCards).Append(typeof(SogginsExeCard));

	internal static readonly Type[] StarterArtifacts = [
		typeof(SmugArtifact),
	];
	internal static readonly Type[] CommonArtifacts = [
		typeof(VideoWillArtifact),
		typeof(PiratedShipCadArtifact),
		typeof(HotTubArtifact),
		typeof(MisprintedApologyArtifact),
	];
	internal static readonly Type[] BossArtifacts = [
		typeof(RepeatedMistakesArtifact),
		typeof(HijinksArtifact),
	];
	internal static readonly Type[] DuoArtifacts = [
		typeof(SogginsDizzyArtifact),
		typeof(SogginsRiggsArtifact),
		typeof(SogginsPeriArtifact),
		typeof(SogginsIsaacArtifact),
		typeof(SogginsDrakeArtifact),
		typeof(SogginsMaxArtifact),
		typeof(SogginsBooksArtifact),
		typeof(SogginsCatArtifact),
	];

	internal static IEnumerable<Type> AllArtifacts
		=> StarterArtifacts.Concat(CommonArtifacts).Concat(BossArtifacts).Concat(DuoArtifacts);

	public void BootMod(IModLoaderContact contact)
	{
		Instance = this;
		KokoroApi = contact.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;
		DuoArtifactsApi = contact.LoadedManifests.Any(m => m.Name == "Shockah.DuoArtifacts") ? contact.GetApi<IDuoArtifactsApi>("Shockah.DuoArtifacts") : null;
		Api = new();
	}

	public void OnNickelLoad(IPluginPackage<Nickel.IModManifest> package, IModHelper helper)
	{
		this.Helper = helper;
		this.Package = package;

		this.FrogproofTrait = helper.Content.Cards.RegisterTrait("Frogproof", new()
		{
			Icon = (_, _) => (Spr)Instance.FrogproofSprite.Id!.Value,
			Name = _ => I18n.FrogproofCardTraitName,
			Tooltips = (_, _) => [Api.FrogproofCardTraitTooltip]
		});

		helper.Content.Cards.OnGetDynamicInnateCardTraitOverrides += (_, e) =>
		{
			if (e.InnateTraits.Contains(FrogproofTrait))
				return;
			if (!Api.IsRunWithSmug(e.State))
				return;
			
			var meta = e.Card.GetMeta();
			if (NewRunOptions.allChars.Contains(meta.deck))
				return;
			if (meta.deck is Deck.colorless or Deck.catartifact or Deck.soggins or Deck.dracula or Deck.tooth or Deck.ephemeral)
				return;
			
			e.SetOverride(FrogproofTrait, true);
		};

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;

			helper.ModRegistry.AwaitApi<IDraculaApi>(
				"Shockah.Dracula",
				api =>
				{
					api.RegisterBloodTapOptionProvider((Status)FrogproofingStatus.Id!.Value, (_, _, status) => [
						new AHurt { targetPlayer = true, hurtAmount = 1 },
						new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
					]);
					api.RegisterBloodTapOptionProvider((Status)ExtraApologiesStatus.Id!.Value, (_, _, status) => [
						new AHurt { targetPlayer = true, hurtAmount = 1 },
						new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
					]);
					api.RegisterBloodTapOptionProvider((Status)ConstantApologiesStatus.Id!.Value, (_, _, status) => [
						new AHurt { targetPlayer = true, hurtAmount = 2 },
						new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
					]);
					api.RegisterBloodTapOptionProvider((Status)BidingTimeStatus.Id!.Value, (_, _, status) => [
						new AHurt { targetPlayer = true, hurtAmount = 1 },
						new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
					]);
					api.RegisterBloodTapOptionProvider((Status)DoublersLuckStatus.Id!.Value, (_, _, status) => [
						new AHurt { targetPlayer = true, hurtAmount = 1 },
						new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
					]);
					api.RegisterBloodTapOptionProvider(Status.backwardsMissiles, (_, _, status) => [
						new AHurt { targetPlayer = true, hurtAmount = 1 },
						new AStatus { targetPlayer = true, status = status, statusAmount = 3 },
					]);
				}
			);

			helper.ModRegistry.AwaitApi<IMoreDifficultiesApi>(
				"TheJazMaster.MoreDifficulties",
				api => api.RegisterAltStarters(
					deck: (Deck)SogginsDeck.Id!.Value,
					starterDeck: new StarterDeck
					{
						cards = [
							new TakeCoverCard(),
							new ThoughtsAndPrayersCard()
						],
						artifacts = [
							new SmugArtifact()
						]
					}
				)
			);

			helper.ModRegistry.AwaitApi<IAppleArtifactApi>(
				"APurpleApple.GenericArtifacts",
				api => api.SetPaletteAction(
					(Deck)SogginsDeck.Id!.Value,
					_ => Instance.KokoroApi.SpoofedActions.MakeAction(
						new AAddCard
						{
							card = new RandomPlaceholderApologyCard(),
							destination = CardDestination.Hand,
							amount = 1
						},
						new AAddApologyCard
						{
							Destination = CardDestination.Hand,
							Amount = 1
						}
					).AsCardAction,
					new TTText(I18n.PaletteTooltip)
				)
			);
		};

		SmugStatusManager = new();
		FrogproofManager = new();
		NarrativeManager = new();
		StatusRenderManager = new();
		StatusLogicManager = new();

		Harmony = new(Name);
		FrogproofManager.ApplyPatches(Harmony);
		SmugStatusManager.ApplyPatches(Harmony);
		SmugPortraitManager.ApplyPatches(Harmony);
		NarrativeManager.ApplyPatches(Harmony);
		Dialogue.ApplyPatches(Harmony);
	}

	public object GetApi(IManifest requestingMod)
		=> new ApiImplementation();

	public void LoadManifest(ISpriteRegistry registry)
	{
		SogginsDeckBorder = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Sprite.DeckBorder",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "DeckBorder.png"))
		);
		ApologyDeckBorder = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Sprite.ApologyDeckBorder",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "ApologyDeckBorder.png"))
		);
		SogginsCharacterBorder = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Sprite.CharacterBorder",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "CharacterBorder.png"))
		);

		MiniPortraitSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Sprite.Portrait.Mini",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Portrait", "Mini.png"))
		);

		List<ExternalSprite>? RegisterAllFrames(string path, string idFormat)
		{
			List<ExternalSprite> frames = [];
			for (int frame = 0; frame < int.MaxValue; frame++)
			{
				var frameFileInfo = new FileInfo(Path.Combine(path, $"{frame}.png"));
				if (!frameFileInfo.Exists)
					break;
				frames.Add(registry.RegisterArtOrThrow(
					id: string.Format(idFormat, frame),
					file: frameFileInfo
				));
			}
			return frames.Count == 0 ? null : frames;
		}

		for (int smugOffset = 0; smugOffset <= Constants.BotchChances.Length / 2; smugOffset++)
		{
			void ActualSmug(int smug)
			{
				var frames = RegisterAllFrames(
					path: Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Portrait", $"Smug{smug}"),
					idFormat: $"{GetType().Namespace}.Sprite.Portrait.Smug.{smug}.Frame.{{0}}"
				);
				if (frames is not null)
					SmugPortraitSprites[smug] = frames;
			}

			if (smugOffset != 0)
				ActualSmug(-smugOffset);
			ActualSmug(smugOffset);
		}
		{
			var frames = RegisterAllFrames(
				path: Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Portrait", "Oversmug"),
				idFormat: $"{GetType().Namespace}.Sprite.Portrait.Smug.Oversmug.Frame.{{0}}"
			);
			if (frames is not null)
				OversmugPortraitSprites.AddRange(frames);
		}
		{
			var frames = RegisterAllFrames(
				path: Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Portrait", "Squint"),
				idFormat: $"{GetType().Namespace}.Sprite.Portrait.Smug.Squint.Frame.{{0}}"
			);
			if (frames is not null)
				SquintPortraitSprites.AddRange(frames);
		}
		{
			var frames = RegisterAllFrames(
				path: Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Portrait", "Mad"),
				idFormat: $"{GetType().Namespace}.Sprite.Portrait.Smug.Mad.Frame.{{0}}"
			);
			if (frames is not null)
				MadPortraitSprites.AddRange(frames);
		}

		SmugStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.Smug",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Status", "Smug.png"))
		);
		FrogproofSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Icon.Frogproof",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "FrogproofIcon.png"))
		);
		BotchesStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.Botches",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Status", "Botches.png"))
		);
		ExtraApologiesStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.ExtraApologies",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Status", "ExtraApologies.png"))
		);
		ConstantApologiesStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.ConstantApologies",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Status", "ConstantApologies.png"))
		);
		BidingTimeStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.BidingTime",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Status", "BidingTime.png"))
		);
		DoubleTimeStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.DoubleTime",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Status", "DoubleTime.png"))
		);
		DoublersLuckStatusSprite = registry.RegisterArtOrThrow(
			id: $"{GetType().Namespace}.Status.DoublersLuck",
			file: new FileInfo(Path.Combine(Instance.ModRootFolder!.FullName, "assets", "Status", "DoublersLuck.png"))
		);

		foreach (var artifactType in AllArtifacts)
		{
			if (DuoArtifacts.Contains(artifactType) && DuoArtifactsApi is null)
				continue;
			if (Activator.CreateInstance(artifactType) is not IRegisterableArtifact artifact)
				continue;
			artifact.RegisterArt(registry);
		}
		foreach (var cardType in AllCards)
		{
			if (Activator.CreateInstance(cardType) is not IRegisterableCard card)
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
			borderSprite: SogginsDeckBorder,
			bordersOverSprite: null
		);
		registry.RegisterDeck(SogginsDeck);

		ApologiesDeck = new(
			globalName: $"{GetType().Namespace}.Deck.Apologies",
			deckColor: System.Drawing.Color.FromArgb(unchecked((int)0xFFB79CE5)), // 0xFF6A9C59
			titleColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF000000)),
			cardArtDefault: ExternalSprite.GetRaw((int)StableSpr.cards_colorless),
			borderSprite: ApologyDeckBorder,
			bordersOverSprite: null
		);
		registry.RegisterDeck(ApologiesDeck);
	}

	public void LoadManifest(IStatusRegistry registry)
	{
		{
			SmugStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.Smug",
				isGood: true,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF483C57)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF483C57)),
				SmugStatusSprite,
				affectedByTimestop: false
			);
			SmugStatus.AddLocalisation(I18n.SmugStatusName, I18n.SmugStatusShortDescription);
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
				isGood: true,
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
				isGood: true,
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
				isGood: true,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF2B5549)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF2B5549)),
				ConstantApologiesStatusSprite,
				affectedByTimestop: false
			);
			ConstantApologiesStatus.AddLocalisation(I18n.ConstantApologiesStatusName, I18n.ConstantApologiesStatusDescription);
			registry.RegisterStatus(ConstantApologiesStatus);
		}
		{
			BidingTimeStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.BidingTime",
				isGood: true,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF639BFF)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF639BFF)),
				BidingTimeStatusSprite,
				affectedByTimestop: true
			);
			BidingTimeStatus.AddLocalisation(I18n.BidingTimeStatusName, I18n.BidingTimeStatusDescription);
			registry.RegisterStatus(BidingTimeStatus);
		}
		{
			DoubleTimeStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.DoubleTime",
				isGood: true,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFFCC503D)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFFCC503D)),
				DoubleTimeStatusSprite,
				affectedByTimestop: false
			);
			DoubleTimeStatus.AddLocalisation(I18n.DoubleTimeStatusName, I18n.DoubleTimeStatusDescription);
			registry.RegisterStatus(DoubleTimeStatus);
		}
		{
			DoublersLuckStatus = new(
				$"{typeof(ModEntry).Namespace}.Status.DoublersLuck",
				isGood: true,
				mainColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF0C560E)),
				borderColor: System.Drawing.Color.FromArgb(unchecked((int)0xFF0C560E)),
				DoublersLuckStatusSprite,
				affectedByTimestop: false
			);
			DoublersLuckStatus.AddLocalisation(I18n.DoublersLuckStatusName, I18n.DoublersLuckStatusDescription);
			registry.RegisterStatus(DoublersLuckStatus);
		}
	}

	public void LoadManifest(IAnimationRegistry registry)
	{
		// all the animations
		foreach (var kvp in SmugPortraitSprites)
		{
			ExternalAnimation animation = new(
				$"{GetType().Namespace}.Animation.Portrait.Smug.{kvp.Key}",
				deck: SogginsDeck,
				tag: $"Smug.{kvp.Key}",
				intendedOverwrite: false,
				frames: kvp.Value
			);
			registry.RegisterAnimation(animation);
			SmugPortraitAnimations[kvp.Key] = animation;
		}
		{
			OversmugPortraitAnimation = new(
				$"{GetType().Namespace}.Animation.Portrait.Oversmug",
				deck: SogginsDeck,
				tag: "Smug.Oversmug",
				intendedOverwrite: false,
				frames: OversmugPortraitSprites
			);
			registry.RegisterAnimation(OversmugPortraitAnimation);
		}
		{
			SquintPortraitAnimation = new(
				$"{GetType().Namespace}.Animation.Portrait.Squint",
				deck: SogginsDeck,
				tag: "squint",
				intendedOverwrite: false,
				frames: SquintPortraitSprites
			);
			registry.RegisterAnimation(SquintPortraitAnimation);
		}
		{
			MadPortraitAnimation = new(
				$"{GetType().Namespace}.Animation.Portrait.Mad",
				deck: SogginsDeck,
				tag: "mad",
				intendedOverwrite: false,
				frames: MadPortraitSprites
			);
			registry.RegisterAnimation(MadPortraitAnimation);
		}
		{
			MiniPortraitAnimation = new(
				$"{GetType().Namespace}.Animation.Portrait.Mini",
				deck: SogginsDeck,
				tag: "mini",
				intendedOverwrite: false,
				frames: [MiniPortraitSprite]
			);
			registry.RegisterAnimation(MiniPortraitAnimation);
		}

		// re-registering the important tags
		{
			NeutralPortraitAnimation = new(
				$"{GetType().Namespace}.Animation.Portrait.Required.neutral",
				deck: SogginsDeck,
				tag: "neutral",
				intendedOverwrite: false,
				frames: SmugPortraitSprites[0]
			);
			registry.RegisterAnimation(NeutralPortraitAnimation);
		}
		{
			GameOverPortraitAnimation = new(
				$"{GetType().Namespace}.Animation.Portrait.Required.gameover",
				deck: SogginsDeck,
				tag: "gameover",
				intendedOverwrite: false,
				frames: SmugPortraitSprites[-2]
			);
			registry.RegisterAnimation(GameOverPortraitAnimation);
		}
	}

	public void LoadManifest(IArtifactRegistry registry)
	{
		foreach (var artifactType in AllArtifacts)
		{
			if (DuoArtifacts.Contains(artifactType) && DuoArtifactsApi is null)
				continue;
			if (Activator.CreateInstance(artifactType) is not IRegisterableArtifact artifact)
				continue;
			artifact.RegisterArtifact(registry);
			artifact.ApplyPatches(Harmony);
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
			charPanelSpr: SogginsCharacterBorder,
			starterDeck: StarterCards,
			starterArtifacts: StarterArtifacts,
			neutralAnimation: NeutralPortraitAnimation,
			miniAnimation: MiniPortraitAnimation
		);
		SogginsCharacter.AddNameLocalisation(I18n.SogginsName);
		SogginsCharacter.AddDescLocalisation(I18n.SogginsDescription);
		registry.RegisterCharacter(SogginsCharacter);

		var entry = Helper.Content.Characters.V2.LookupByDeck((Deck)SogginsDeck.Id!.Value)!;
		entry.Amend(new() { ExeCardType = typeof(SogginsExeCard) });
	}
}
