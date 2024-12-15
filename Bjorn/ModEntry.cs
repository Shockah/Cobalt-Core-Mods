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
	internal readonly HookManager<IBjornApi.IHook> HookManager;
	internal readonly MultiPool ArgsPool;
	internal readonly IKokoroApi.IV2 KokoroApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal IDeckEntry BjornDeck { get; }

	private static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(DrawConclusionsCard),
		typeof(ElectronGunCard),
		typeof(EntangleCard),
		typeof(FractalStructureCard),
		typeof(LorentzTransformCard),
		typeof(PrototypingCard),
		typeof(SafetyProtocolCard),
		typeof(SmartShieldDroneCard),
		typeof(TaserCard),
	];

	private static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(AssimilateCard),
		typeof(FieldTestCard),
		typeof(HandheldDuplitronCard),
		typeof(InsuranceCard),
		typeof(ReevaluationCard),
		typeof(RelativityCard),
		typeof(RepulsiveForceCard),
	];

	private static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(AccelerateCard),
		typeof(CrystalKnowledgeCard),
		typeof(LilHadronColliderCard),
		typeof(NeglectSafetyCard),
		typeof(WaterfallModelCard),
	];

	private static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			typeof(PrototypeCard),
			//typeof(BlochExeCard),
		];

	private static readonly IReadOnlyList<Type> CommonArtifacts = [
		typeof(FourDChessArtifact),
		typeof(RelativityTheoryArtifact),
		typeof(SpecialRelativityArtifact),
	];

	private static readonly IReadOnlyList<Type> BossArtifacts = [
		typeof(OutsideTheBoxArtifact),
		typeof(ScientificMethodArtifact),
	];

	private static readonly IReadOnlyList<Type> DuoArtifacts = [
	];

	private static readonly IEnumerable<Type> AllArtifactTypes
		= [
			.. CommonArtifacts,
			.. BossArtifacts,
		];

	private static readonly IEnumerable<Type> RegisterableTypes
		= [
			.. AllCardTypes,
			.. AllArtifactTypes,
			.. DuoArtifacts,
			typeof(AcceleratedManager),
			typeof(AnalyzeManager),
			typeof(CrystalKnowledgeManager),
			typeof(EntanglementManager),
			typeof(GadgetManager),
			typeof(RelativityManager),
			typeof(SmartShieldManager),
			typeof(SmartShieldDrone),
		];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		HookManager = new(package.Manifest.UniqueName);
		ArgsPool = new();
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
	
	internal IEnumerable<IBjornApi.IHook> FilterIsCanAnalyzeHookEnabled(
		State state,
		Combat combat,
		Card card,
		IEnumerable<IBjornApi.IHook> enumerable
	)
	{
		var args = ArgsPool.Get<ApiImplementation.IsCanAnalyzeHookEnabledArgs>();
		try
		{
			args.State = state;
			args.Combat = combat;
			args.Card = card;

			var hooks = Instance.HookManager.GetHooksWithProxies(Helper.Utilities.ProxyManager, state.EnumerateAllArtifacts()).ToList();
			return enumerable.Where(hook =>
			{
				args.Hook = hook;
				return hooks.All(h => h.IsCanAnalyzeHookEnabled(args));
			});
		}
		finally
		{
			ArgsPool.Return(args);
		}
	}
}