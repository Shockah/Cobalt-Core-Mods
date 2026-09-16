using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Shockah.Kokoro;

namespace Shockah.NewLight;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly IKokoroApi.IV2 KokoroApi;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal readonly IDeckEntry GuardianDeck;
	
	private static readonly IEnumerable<Type> SpecialCardTypes = [
		typeof(AbilitiesCard),
	];
	
	private static readonly IEnumerable<Type> AbilityTypes = [
		typeof(BurstGlideCard),
		typeof(CatapultLiftCard),
		typeof(HealingRiftCard),
		typeof(TripleJumpCard),
	];
	
	private static readonly IEnumerable<Type> LegendaryWeaponTypes = [
		typeof(AdaptiveGrenadeLauncherCard),
		typeof(AdaptiveSniperRifleCard),
		typeof(AdaptiveSidearmCard),
		typeof(AggressiveShotgunCard),
		typeof(AutoRifleCard),
		typeof(BurstSidearmCard),
		typeof(CombatBowCard),
		typeof(HandCannonCard),
		typeof(HighImpactFusionRifleCard),
		typeof(HighImpactSniperRifleCard),
		typeof(LightweightGrenadeLauncherCard),
		typeof(LightweightSwordCard),
		typeof(LinearFusionRifleCard),
		typeof(MachineGunCard),
		typeof(MissilePulseRifleCard),
		typeof(PulseRifleCard),
		typeof(RapidFireFusionRifleCard),
		typeof(RapidFireShotgunCard),
		typeof(RocketLauncherCard),
		typeof(ScoutRifleCard),
		typeof(SubmachineGunCard),
		typeof(TraceRifleCard),
		typeof(VortexSwordCard),
		typeof(WaveGrenadeLauncherCard),
	];
	
	private static readonly IEnumerable<Type> ExoticWeaponTypes = [
		typeof(FafnirCard),
		typeof(FourthHorsemanCard),
		typeof(IzanagisBurdenCard),
		typeof(OutbreakPerfectedCard),
		typeof(ThunderlordCard),
	];
	
	private static readonly IEnumerable<Type> WeaponPerkTypes = [
		typeof(AutoLoadingHolsterWeaponPerk),
		typeof(ClownCartridgeWeaponPerk),
		typeof(CompulsiveReloaderWeaponPerk),
		typeof(DemoralizeWeaponPerk),
		typeof(EnviousArsenalWeaponPerk),
		typeof(FrenzyWeaponPerk),
		typeof(HeadstoneWeaponPerk),
		typeof(HealClipWeaponPerk),
		typeof(QuickdrawWeaponPerk),
		typeof(RampageWeaponPerk),
		typeof(RepulsorBraceWeaponPerk),
		typeof(RimestealerWeaponPerk),
		typeof(SurroundedWeaponPerk),
		typeof(VorpalWeaponWeaponPerk),
	];
	
	private static readonly IEnumerable<Type> FeatureTypes = [
		typeof(Abilities),
		typeof(Ammo),
		typeof(BlastAction),
		typeof(FullAutoCardTrait),
		typeof(NegativeHermesBoots),
		typeof(PrecisionCardTrait),
		typeof(ScatterAction),
		typeof(TensionCardTrait),
		typeof(WaveAction),
		typeof(WeaponCard),
		.. WeaponPerkTypes,
		typeof(LegendaryWeaponCard), // last for dependency purposes
	];
	
	private static readonly IEnumerable<Type> RegisterableTypes = [
		typeof(GhostArtifact),
		.. FeatureTypes,
		.. LegendaryWeaponTypes,
		.. ExoticWeaponTypes,
		.. AbilityTypes,
		.. SpecialCardTypes,
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
		
		GuardianDeck = helper.Content.Decks.RegisterDeck("Guardian", new()
		{
			Definition = new() { color = new("FFFFFF"), titleColor = Colors.white },
			DefaultCardArt = StableSpr.cards_colorless,
			// BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			BorderSprite = StableSpr.cardShared_border_ephemeral,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize,
			ShineColorOverride = args => DB.decks[args.Card.GetMeta().deck].color.normalize().gain(0.5),
			CardFrameOverride = args =>
			{
				if (args.Card is WeaponCard weapon)
					return weapon.OverrideCardFrame(args);
				return args.DefaultFrameSprite;
			},
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}
}