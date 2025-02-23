using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.Common;
using Shockah.Kokoro;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dyna;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly Harmony Harmony;
	internal readonly HookManager<IDynaHook> HookManager;
	internal readonly ApiImplementation Api;
	internal readonly IKokoroApi.IV2 KokoroApi;
	internal readonly IDuoArtifactsApi? DuoArtifactsApi;
	internal readonly IMoreDifficultiesApi? MoreDifficultiesApi;
	internal readonly ISogginsApi? SogginsApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal IDeckEntry DynaDeck { get; }

	internal static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(BangCard),
		typeof(BurstChargeCard),
		typeof(ClearAPathCard),
		typeof(DemoChargeCard),
		typeof(FlashBurstCard),
		typeof(FluxChargeCard),
		typeof(IncomingCard),
		typeof(KaboomCard),
		typeof(SwiftChargeCard),
	];

	internal static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(BlitzkriegCard),
		typeof(BunkerCard),
		typeof(LightItUpCard),
		typeof(LockAndLoadCard),
		typeof(PerkUpCard),
		typeof(RemoteDetonatorCard),
		typeof(SmokeBombCard),
	];

	internal static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(BastionCard),
		typeof(ConcussionChargeCard),
		typeof(MegatonBlastCard),
		typeof(NitroCard),
		typeof(ShatterChargeCard),
	];

	internal static readonly IReadOnlyList<Type> SpecialCardTypes = [
		typeof(CustomChargeCard),
		typeof(DynaExeCard),
	];

	internal static IEnumerable<Type> AllCardTypes
		=> CommonCardTypes
			.Concat(UncommonCardTypes)
			.Concat(RareCardTypes)
			.Concat(SpecialCardTypes);

	internal static readonly IReadOnlyList<Type> CommonArtifacts = [
		typeof(BlastPowderArtifact),
		typeof(FirecrackerArtifact),
		typeof(GeligniteArtifact),
		typeof(HardHatArtifact),
		typeof(VolatileFuseArtifact),
	];

	internal static readonly IReadOnlyList<Type> BossArtifacts = [
		typeof(PyromaniaArtifact),
		typeof(UnstableCompoundArtifact),
	];

	internal static readonly IReadOnlyList<Type> DuoArtifacts = [
		typeof(DynaBooksArtifact),
		typeof(DynaCatArtifact),
		typeof(DynaDizzyArtifact),
		typeof(DynaDrakeArtifact),
		typeof(DynaEddieArtifact),
		typeof(DynaIsaacArtifact),
		typeof(DynaMaxArtifact),
		typeof(DynaPeriArtifact),
		typeof(DynaRiggsArtifact),
		typeof(DynaSogginsArtifact),
	];

	internal static readonly IEnumerable<Type> AllArtifactTypes
		= [
			.. CommonArtifacts,
			.. BossArtifacts,
		];

	internal static readonly IReadOnlyList<Type> ChargeTypes = [
		typeof(BurstCharge),
		typeof(ConcussionCharge),
		typeof(DemoCharge),
		typeof(FluxCharge),
		typeof(ShatterCharge),
		typeof(SwiftCharge),
	];

	internal static readonly IEnumerable<Type> RegisterableTypes
		= AllCardTypes.Concat(AllArtifactTypes).Concat(ChargeTypes).Concat(DuoArtifacts);

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);
		HookManager = new(package.Manifest.UniqueName);
		Api = new();
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;
		DuoArtifactsApi = helper.ModRegistry.GetApi<IDuoArtifactsApi>("Shockah.DuoArtifacts");
		SogginsApi = helper.ModRegistry.GetApi<ISogginsApi>("Shockah.Soggins");

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		_ = new BlastwaveManager();
		_ = new ChargeManager();
		_ = new NitroManager();
		_ = new BastionManager();
		_ = new FluxPartModManager();
		//_ = new JesterIntegration();

		DynaDeck = helper.Content.Decks.RegisterDeck("Dyna", new()
		{
			Definition = new() { color = new("EC592B"), titleColor = Colors.black },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		helper.Content.Characters.V2.RegisterPlayableCharacter("Dyna", new()
		{
			Deck = DynaDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CharacterFrame.png")).Sprite,
			NeutralAnimation = new()
			{
				CharacterType = DynaDeck.UniqueName,
				LoopTag = "neutral",
				Frames = Enumerable.Range(0, 1)
					.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Neutral/{i}.png")).Sprite)
					.ToList()
			},
			MiniAnimation = new()
			{
				CharacterType = DynaDeck.UniqueName,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini.png")).Sprite
				]
			},
			Starters = new()
			{
				cards = [
					new DemoChargeCard(),
					new KaboomCard()
				]
			},
			ExeCardType = typeof(DynaExeCard)
		});

		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = DynaDeck.UniqueName,
			LoopTag = "gameover",
			Frames = Enumerable.Range(0, 1)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/GameOver/{i}.png")).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = DynaDeck.UniqueName,
			LoopTag = "squint",
			Frames = Enumerable.Range(0, 2)
				.Select(i => helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile($"assets/Character/Squint/{i}.png")).Sprite)
				.ToList()
		});

		MoreDifficultiesApi = helper.ModRegistry.GetApi<IMoreDifficultiesApi>("TheJazMaster.MoreDifficulties", new SemanticVersion(1, 3, 0));
		MoreDifficultiesApi?.RegisterAltStarters(
			deck: DynaDeck.Deck,
			starterDeck: new StarterDeck
			{
				cards = [
					new BangCard(),
					new BurstChargeCard()
				]
			}
		);

		helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;

			if (helper.ModRegistry.GetApi<IDraculaApi>("Shockah.Dracula") is { } draculaApi)
			{
				draculaApi.RegisterBloodTapOptionProvider(BastionManager.BastionStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 1 },
					new AStatus { targetPlayer = true, status = Status.shield, statusAmount = 1 }
				]);
				draculaApi.RegisterBloodTapOptionProvider(NitroManager.TempNitroStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 2 }
				]);
				draculaApi.RegisterBloodTapOptionProvider(NitroManager.NitroStatus.Status, (_, _, status) => [
					new AHurt { targetPlayer = true, hurtAmount = 1 },
					new AStatus { targetPlayer = true, status = status, statusAmount = 1 }
				]);
			}

			helper.ModRegistry.GetApi<IAppleShipyardApi>("APurpleApple.Shipyard", new SemanticVersion(1, 6, 7))?.RegisterActionLooksForPartType(typeof(FireChargeAction), PType.missiles);
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
