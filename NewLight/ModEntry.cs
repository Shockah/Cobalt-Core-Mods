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
	
	private static readonly IEnumerable<Type> LegendaryWeaponTypes = [
		typeof(AdaptiveSidearmCard),
		typeof(AutoRifleCard),
		typeof(BurstSidearmCard),
		typeof(HandCannonCard),
		typeof(MachineGunCard),
		typeof(PulseRifleCard),
		typeof(SubmachineGunCard),
		typeof(TraceRifleCard),
	];
	
	private static readonly IEnumerable<Type> WeaponPerkTypes = [
		typeof(FrenzyWeaponPerk),
		typeof(SurroundedWeaponPerk),
		typeof(VorpalWeaponWeaponPerk),
	];
	
	private static readonly IEnumerable<Type> FeatureTypes = [
		typeof(Ammo),
		typeof(FullAutoCardTrait),
		.. WeaponPerkTypes,
		typeof(LegendaryWeaponCard),
	];
	
	private static readonly IEnumerable<Type> RegisterableTypes = [
		typeof(GhostArtifact),
		.. FeatureTypes,
		.. LegendaryWeaponTypes,
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
			Definition = new() { color = new("C2FF60"), titleColor = Colors.white },
			DefaultCardArt = StableSpr.cards_colorless,
			// BorderSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/CardFrame.png")).Sprite,
			BorderSprite = StableSpr.cardShared_border_ephemeral,
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize,
			CardFrameOverride = args =>
			{
				if (args.Card is LegendaryWeaponCard legendary)
					return legendary.OverrideCardFrame(args);
				return args.DefaultFrameSprite;
			},
		});

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);
	}
}