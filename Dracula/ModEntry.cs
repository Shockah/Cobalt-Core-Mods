using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Common;
using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	internal IHarmony Harmony { get; }
	internal IKokoroApi.IV2 KokoroApi { get; }
	internal IEssentialsApi? EssentialsApi { get; private set; }
	internal IDuoArtifactsApi? DuoArtifactsApi { get; private set; }
	internal IDynaApi? DynaApi { get; private set; }
	internal IJohnsonApi? JohnsonApi { get; private set; }
	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	internal BloodTapManager BloodTapManager { get; }

	internal IDeckEntry DraculaDeck { get; }
	internal IDeckEntry BatmobileDeck { get; }

	internal IStatusEntry BleedingStatus { get; }
	internal IStatusEntry BloodMirrorStatus { get; }
	internal IStatusEntry TransfusionStatus { get; }
	internal IStatusEntry TransfusingStatus { get; }

	internal ISpriteEntry BleedingCostOff { get; }
	internal ISpriteEntry BleedingCostOn { get; }
	internal ISpriteEntry HullBelowHalf { get; }
	internal ISpriteEntry HullAboveHalf { get; }

	internal ISpriteEntry BatIcon { get; }
	internal ISpriteEntry BatAIcon { get; }
	internal ISpriteEntry BatBIcon { get; }
	internal ISpriteEntry BatSprite { get; }
	internal ISpriteEntry BatASprite { get; }
	internal ISpriteEntry BatBSprite { get; }
	internal ISpriteEntry DroneTriggerIcon { get; }

	internal IShipEntry Ship { get; }
	internal IPartEntry ShipWing { get; }
	internal IPartEntry ShipArmoredWing { get; }

	private readonly HashSet<Type> ExeTypes = [];

	internal static IReadOnlyList<Type> CommonCardTypes { get; } = [
		typeof(BiteCard),
		typeof(BloodShieldCard),
		typeof(ClonedLeechCard),
		typeof(DrainEssenceCard),
		typeof(BatFormCard),
		typeof(BloodMirrorCard),
		typeof(GrimoireOfSecretsCard),
		typeof(SummonBatCard),
		typeof(DeathCoilCard),
	];

	internal static IReadOnlyList<Type> UncommonCardTypes { get; } = [
		typeof(AuraOfDarknessCard),
		typeof(HeartbreakCard),
		typeof(BloodScentCard),
		typeof(DispersionCard),
		typeof(EnshroudCard),
		typeof(EcholocationCard),
		typeof(DominateCard),
	];

	internal static IReadOnlyList<Type> RareCardTypes { get; } = [
		typeof(ScreechCard),
		typeof(RedThirstCard),
		typeof(CrimsonWaveCard),
		typeof(BloodTapCard),
		typeof(SacrificeCard),
	];

	internal static IReadOnlyList<Type> SecretAttackCardTypes { get; } = [
		typeof(SecretViolentCard),
		typeof(SecretPerforatingCard),
		typeof(SecretPiercingCard),
	];

	internal static IReadOnlyList<Type> SecretNonAttackCardTypes { get; } = [
		typeof(SecretProtectiveCard),
		typeof(SecretVigorousCard),
		typeof(SecretRestorativeCard),
		typeof(SecretWingedCard),
	];

	internal static IReadOnlyList<Type> ShipCards { get; } = [
		typeof(BatmobileBasicRepairsCard),
		typeof(BatDebitCard),
	];

	internal static IEnumerable<Type> AllCardTypes = [
		..CommonCardTypes,
		..UncommonCardTypes,
		..RareCardTypes,
		typeof(DraculaExeCard),
		typeof(PlaceholderSecretCard),
		..SecretAttackCardTypes,
		..SecretNonAttackCardTypes,
		..ShipCards
	];

	internal static IReadOnlyList<Type> CommonArtifacts { get; } = [
		typeof(MasochismArtifact),
		typeof(ThinBloodArtifact),
		typeof(WingsOfNightArtifact),
		typeof(TheCountArtifact),
	];

	internal static IReadOnlyList<Type> BossArtifacts { get; } = [
		typeof(DanseMacabreArtifact),
		typeof(PurgatoryArtifact),
	];

	internal static IReadOnlyList<Type> ShipArtifacts { get; } = [
		typeof(BatmobileArtifact),
		typeof(BloodBankArtifact),
		typeof(ABTypeArtifact),
		typeof(OTypeArtifact),
	];

	internal static IEnumerable<Type> AllArtifactTypes
		=> CommonArtifacts.Concat(BossArtifacts).Concat(ShipArtifacts);

	internal static IReadOnlyList<Type> DuoArtifactTypes { get; } = [
		typeof(DraculaBooksArtifact),
		typeof(DraculaCatArtifact),
		typeof(DraculaDizzyArtifact),
		typeof(DraculaDrakeArtifact),
		typeof(DraculaDynaArtifact),
		typeof(DraculaIsaacArtifact),
		typeof(DraculaJohnsonArtifact),
	];

	internal static readonly IEnumerable<Type> RegisterableTypes
		= [..AllCardTypes, ..AllArtifactTypes];

	internal static readonly IEnumerable<Type> LateRegisterableTypes
		= DuoArtifactTypes;

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;
		_ = new BleedingManager();
		_ = new BloodMirrorManager();
		_ = new LifestealManager();
		_ = new TransfusionManager();
		_ = new NegativeOverdriveManager();
		_ = new CardScalingManager();
		BloodTapManager = new();
		CardSelectFilters.Register(package, helper);

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;

			DuoArtifactsApi = helper.ModRegistry.GetApi<IDuoArtifactsApi>("Shockah.DuoArtifacts");
			EssentialsApi = helper.ModRegistry.GetApi<IEssentialsApi>("Nickel.Essentials");
			DynaApi = helper.ModRegistry.GetApi<IDynaApi>("Shockah.Dyna");
			JohnsonApi = helper.ModRegistry.GetApi<IJohnsonApi>("Shockah.Johnson");

			foreach (var registerableType in LateRegisterableTypes)
				AccessTools.DeclaredMethod(registerableType, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
		};

		DynamicWidthCardAction.ApplyPatches(Harmony, logger);
		ASpecificCardOffering.ApplyPatches(Harmony, logger);

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		var defaultCardArt = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Cards/Default.png")).Sprite;
		DraculaDeck = helper.Content.Decks.RegisterDeck("Dracula", new()
		{
			Definition = new() { color = DB.decks[Deck.dracula].color, titleColor = DB.decks[Deck.dracula].titleColor },
			DefaultCardArt = defaultCardArt,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});
		BatmobileDeck = helper.Content.Decks.RegisterDeck("Batmobile", new()
		{
			Definition = new() { color = DB.decks[Deck.dracula].color, titleColor = DB.decks[Deck.dracula].titleColor },
			DefaultCardArt = defaultCardArt,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/BatmobileCardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["ship", "name"]).Localize
		});

		BleedingCostOff = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/BleedingCostOff.png"));
		BleedingCostOn = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/BleedingCostOn.png"));
		HullBelowHalf = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/HullBelowHalf.png"));
		HullAboveHalf = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/HullAboveHalf.png"));

		BatIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/Bat.png"));
		BatAIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/BatA.png"));
		BatBIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/BatB.png"));
		BatSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Bat.png"));
		BatASprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/BatA.png"));
		BatBSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/BatB.png"));
		DroneTriggerIcon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/DroneTrigger.png"));

		BleedingStatus = helper.Content.Statuses.RegisterStatus("Bleeding", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/Bleeding.png")).Sprite,
				color = new("BE0000"),
				affectedByTimestop = true,
				isGood = false,
			},
			Name = this.AnyLocalizations.Bind(["status", "Bleeding", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "Bleeding", "description"]).Localize
		});
		BloodMirrorStatus = helper.Content.Statuses.RegisterStatus("BloodMirror", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/BloodMirror.png")).Sprite,
				color = new("FF7F7F"),
				affectedByTimestop = true,
				isGood = true,
			},
			Name = this.AnyLocalizations.Bind(["status", "BloodMirror", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "BloodMirror", "description"]).Localize
		});
		TransfusionStatus = helper.Content.Statuses.RegisterStatus("Transfusion", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/Transfusion.png")).Sprite,
				color = new("FFAFAF"),
				isGood = true,
			},
			Name = this.AnyLocalizations.Bind(["status", "Transfusion", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "Transfusion", "description"]).Localize
		});
		TransfusingStatus = helper.Content.Statuses.RegisterStatus("Transfusing", new()
		{
			Definition = new()
			{
				icon = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/Transfusing.png")).Sprite,
				color = new("FFAFAF"),
				isGood = true,
			},
			Name = this.AnyLocalizations.Bind(["status", "Transfusing", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "Transfusing", "description"]).Localize
		});

		foreach (var registerableType in RegisterableTypes)
			AccessTools.DeclaredMethod(registerableType, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		helper.Content.Characters.V2.RegisterPlayableCharacter("Dracula", new()
		{
			Deck = DraculaDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			NeutralAnimation = new()
			{
				CharacterType = DraculaDeck.UniqueName,
				LoopTag = "neutral",
				Frames = [
					StableSpr.characters_dracula_dracula_neutral_0,
					StableSpr.characters_dracula_dracula_neutral_1,
					StableSpr.characters_dracula_dracula_neutral_2,
					StableSpr.characters_dracula_dracula_neutral_3,
					StableSpr.characters_dracula_dracula_neutral_4,
				]
			},
			MiniAnimation = new()
			{
				CharacterType = DraculaDeck.UniqueName,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			},
			Starters = new()
			{
				cards = [
					new BiteCard(),
					new BloodShieldCard()
				]
			},
			ExeCardType = typeof(DraculaExeCard)
		});

		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = DraculaDeck.UniqueName,
			LoopTag = "gameover",
			Frames = Enumerable.Range(0, 1)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/GameOver/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = DraculaDeck.UniqueName,
			LoopTag = "squint",
			Frames = Enumerable.Range(0, 5)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Squint/{i}.png")).Sprite)
				.ToList()
		});

		ShipWing = helper.Content.Ships.RegisterPart("Batmobile.Wing", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Ship/Wing.png")).Sprite
		});
		ShipArmoredWing = helper.Content.Ships.RegisterPart("Batmobile.Wing.Armored", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Ship/WingArmored.png")).Sprite
		});
		var shipCockpit = helper.Content.Ships.RegisterPart("Batmobile.Cockpit", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Ship/Cockpit.png")).Sprite
		});
		var shipCannon = helper.Content.Ships.RegisterPart("Batmobile.Cannon", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Ship/Cannon.png")).Sprite
		});
		var shipBay = helper.Content.Ships.RegisterPart("Batmobile.Bay", new()
		{
			Sprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Ship/Bay.png")).Sprite
		});

		Ship = helper.Content.Ships.RegisterShip("Batmobile", new()
		{
			Ship = new()
			{
				ship = new Ship
				{
					hull = 12,
					hullMax = 12,
					shieldMaxBase = 3,
					parts =
					{
						new Part
						{
							type = PType.wing,
							skin = ShipWing.UniqueName,
							damageModifier = PDamMod.weak
						},
						new Part
						{
							type = PType.cockpit,
							skin = shipCockpit.UniqueName
						},
						new Part
						{
							type = PType.cannon,
							skin = shipCannon.UniqueName
						},
						new Part
						{
							type = PType.missiles,
							skin = shipBay.UniqueName
						},
						new Part
						{
							type = PType.wing,
							skin = ShipWing.UniqueName,
							damageModifier = PDamMod.weak,
							flip = true
						}
					}
				},
				artifacts =
				{
					new ShieldPrep(),
					new BatmobileArtifact(),
					new BloodBankArtifact(),
				},
				cards =
				{
					new CannonColorless(),
					new DodgeColorless(),
					new BasicShieldColorless(),
					new BatmobileBasicRepairsCard(),
				}
			},
			UnderChassisSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Ship/Chassis.png")).Sprite,
			ExclusiveArtifactTypes = new HashSet<Type>()
			{
				typeof(BatmobileArtifact),
				typeof(BloodBankArtifact),
				typeof(ABTypeArtifact),
				typeof(OTypeArtifact),
			},
			Name = this.AnyLocalizations.Bind(["ship", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["ship", "description"]).Localize,
		});

		BloodTapManager.RegisterOptionProvider(BloodMirrorStatus.Status, (_, _, status) => [
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
			new AHurt { targetPlayer = true, hurtAmount = 1 },
		]);
		BloodTapManager.RegisterOptionProvider(TransfusionStatus.Status, (_, _, status) => [
			new AHurt { targetPlayer = true, hurtAmount = 1 },
			new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
		]);

		helper.ModRegistry.GetApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties", new SemanticVersion(1, 3, 0))?.RegisterAltStarters(
			deck: DraculaDeck.Deck,
			starterDeck: new StarterDeck
			{
				cards = [
					new SummonBatCard(),
					new BatFormCard()
				]
			}
		);
	}

	public override object? GetApi(IModManifest requestingMod)
		=> new ApiImplementation();

	private void UpdateExeTypesIfNeeded()
	{
		if (ExeTypes.Count != 0)
			return;

		foreach (var deck in NewRunOptions.allChars)
			if (Helper.Content.Characters.V2.LookupByDeck(deck) is { } character)
				if (character.Configuration.ExeCardType is { } exeCardType)
					ExeTypes.Add(exeCardType);
	}

	internal IEnumerable<Type> GetExeCardTypes()
	{
		if (EssentialsApi is { } essentialsApi)
		{
			foreach (var deck in NewRunOptions.allChars)
				if (essentialsApi.GetExeCardTypeForDeck(deck) is { } exeCardType)
					yield return exeCardType;
		}
		else
		{
			UpdateExeTypesIfNeeded();
			foreach (var exeCardType in ExeTypes)
				yield return exeCardType;
		}
	}

	internal bool IsExeCardType(Type cardType)
	{
		if (EssentialsApi is { } essentialsApi)
			return essentialsApi.IsExeCardType(cardType);

		UpdateExeTypesIfNeeded();
		return ExeTypes.Contains(cardType);
	}
}