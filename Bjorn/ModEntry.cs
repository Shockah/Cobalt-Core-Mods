using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System.Collections.Generic;
using System;
using HarmonyLib;
using Shockah.Kokoro;
using System.Linq;
using Nickel.Common;
using Shockah.MORE;
using Shockah.Shared;
using TheJazMaster.MoreDifficulties;
using TheJazMaster.TyAndSasha;

namespace Shockah.Bjorn;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly HookManager<IBjornApi.IHook> HookManager;
	internal readonly MultiPool ArgsPool;
	internal readonly IKokoroApi.IV2 KokoroApi;
	internal ITyAndSashaApi? TyAndSashaApi { get; private set; }
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal IDeckEntry BjornDeck { get; }

	private static readonly IReadOnlyList<Type> CommonCardTypes = [
		typeof(AssimilateCard),
		typeof(ElectronGunCard),
		typeof(EntangleCard),
		typeof(FractalStructureCard),
		typeof(LorentzTransformCard),
		typeof(SafetyProtocolCard),
		typeof(SmartShieldDroneCard),
		typeof(TaserCard),
		typeof(ThesisCard),
	];

	private static readonly IReadOnlyList<Type> UncommonCardTypes = [
		typeof(AdjustCard),
		typeof(ConclusionsCard),
		typeof(FieldTestCard),
		typeof(HandheldDuplitronCard),
		typeof(InsuranceCard),
		typeof(RelativityCard),
		typeof(RepulsiveForceCard),
	];

	private static readonly IReadOnlyList<Type> RareCardTypes = [
		typeof(AccelerateCard),
		typeof(ContingencyPlanCard),
		typeof(LilHadronColliderCard),
		typeof(NeglectSafetyCard),
		typeof(WaterfallModelCard),
	];

	private static readonly IEnumerable<Type> AllCardTypes
		= [
			.. CommonCardTypes,
			.. UncommonCardTypes,
			.. RareCardTypes,
			typeof(TerminateCard),
			//typeof(BlochExeCard),
		];

	private static readonly IReadOnlyList<Type> CommonArtifacts = [
		typeof(OvertimeArtifact),
		typeof(SideProjectsArtifact),
		typeof(SpecialRelativityArtifact),
		typeof(SynchrotronArtifact),
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
		helper.ModRegistry.AwaitApi<ITyAndSashaApi>("TheJazMaster.TyAndSasha", api => TyAndSashaApi = api);

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
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize,
			ShineColorOverride = _ => DB.decks[BjornDeck!.Deck].color.normalize().gain(2.5),
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
		
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = BjornDeck.UniqueName,
			LoopTag = "gameover",
			Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/GameOver")
				.GetSequentialFiles(i => $"{i}.png")
				.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
				.ToList()
		});
		helper.Content.Characters.V2.RegisterCharacterAnimation(new()
		{
			CharacterType = BjornDeck.UniqueName,
			LoopTag = "squint",
			Frames = package.PackageRoot.GetRelativeDirectory("assets/Character/Squint")
				.GetSequentialFiles(i => $"{i}.png")
				.Select(f => helper.Content.Sprites.RegisterSprite(f).Sprite)
				.ToList()
		});

		helper.ModRegistry.AwaitApi<IMoreDifficultiesApi>(
			"TheJazMaster.MoreDifficulties",
			new SemanticVersion(1, 3, 0),
			api => api.RegisterAltStarters(
				deck: BjornDeck.Deck,
				starterDeck: new StarterDeck
				{
					cards = [
						new SmartShieldDroneCard(),
						new TaserCard(),
					]
				}
			)
		);
		
		helper.ModRegistry.AwaitApi<IMoreApi>(
			"Shockah.MORE",
			api =>
			{
				api.RegisterAltruisticArtifact(SpecialRelativityArtifact.Entry.UniqueName);
				api.RegisterAltruisticArtifact(SynchrotronArtifact.Entry.UniqueName);
			}
		);
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation();

	internal static Rarity GetCardRarity(Type type)
	{
		if (type == typeof(TerminateCard))
			return Rarity.rare;
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