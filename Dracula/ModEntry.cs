using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	internal Harmony Harmony { get; }
	internal IKokoroApi KokoroApi { get; }
	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	internal IDeckEntry DraculaDeck { get; }
	internal IStatusEntry BleedingStatus { get; }
	internal IStatusEntry BloodMirrorStatus { get; }
	internal IStatusEntry TransfusionStatus { get; }
	internal IStatusEntry TransfusingStatus { get; }

	internal ISpriteEntry ShieldCostOff { get; }
	internal ISpriteEntry ShieldCostOn { get; }
	internal ISpriteEntry BleedingCostOff { get; }
	internal ISpriteEntry BleedingCostOn { get; }

	internal static IReadOnlyList<Type> StarterCardTypes { get; } = [
		typeof(BiteCard),
		typeof(BloodShieldCard),
	];

	internal static IReadOnlyList<Type> CommonCardTypes { get; } = [
		typeof(ClonedLeechCard),
		typeof(DrainEssenceCard),
		typeof(BatFormCard),
		typeof(BloodMirrorCard),
		typeof(GrimoireOfSecretsCard),
	];

	internal static IReadOnlyList<Type> UncommonCardTypes { get; } = [
		typeof(AuraOfDarknessCard),
		typeof(HeartbreakCard),
		typeof(BloodScentCard),
		typeof(DispersionCard),
	];

	internal static IReadOnlyList<Type> RareCardTypes { get; } = [
		typeof(ScreechCard),
		typeof(RedThirstCard),
		typeof(CrimsonWaveCard),
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
	];

	internal static IEnumerable<Type> AllCardTypes
		=> StarterCardTypes
			.Concat(CommonCardTypes)
			.Concat(UncommonCardTypes)
			.Concat(RareCardTypes)
			.Append(typeof(PlaceholderSecretCard))
			.Concat(SecretAttackCardTypes)
			.Concat(SecretNonAttackCardTypes);

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = new(package.Manifest.UniqueName);
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!;
		KokoroApi.RegisterTypeForExtensionData(typeof(AHurt));
		KokoroApi.RegisterTypeForExtensionData(typeof(AAttack));
		_ = new BleedingManager();
		_ = new BloodMirrorManager();
		_ = new LifestealManager();
		_ = new TransfusionManager();
		//KokoroApi.RegisterCardRenderHook(new SpacingCardRenderHook(), 0);

		ASpecificCardOffering.ApplyPatches(Harmony, logger);

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		DraculaDeck = Helper.Content.Decks.RegisterDeck("Dracula", new()
		{
			Definition = new() { color = DB.decks[Deck.dracula].color, titleColor = DB.decks[Deck.dracula].titleColor },
			DefaultCardArt = StableSpr.cards_colorless,
			BorderSprite = DB.deckBorders[Deck.dracula],
			Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
		});

		ShieldCostOff = Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/ShieldCostOff.png"));
		ShieldCostOn = Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/ShieldCostOn.png"));
		BleedingCostOff = Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/BleedingCostOff.png"));
		BleedingCostOn = Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Icons/BleedingCostOn.png"));

		BleedingStatus = Helper.Content.Statuses.RegisterStatus("Bleeding", new()
		{
			Definition = new()
			{
				icon = Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/Bleeding.png")).Sprite,
				color = new("BE0000")
			},
			Name = this.AnyLocalizations.Bind(["status", "Bleeding", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "Bleeding", "description"]).Localize
		});
		BloodMirrorStatus = Helper.Content.Statuses.RegisterStatus("BloodMirror", new()
		{
			Definition = new()
			{
				icon = Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/BloodMirror.png")).Sprite,
				color = new("FF7F7F")
			},
			Name = this.AnyLocalizations.Bind(["status", "BloodMirror", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "BloodMirror", "description"]).Localize
		});
		TransfusionStatus = Helper.Content.Statuses.RegisterStatus("Transfusion", new()
		{
			Definition = new()
			{
				icon = Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/Transfusion.png")).Sprite,
				color = new("267F00")
			},
			Name = this.AnyLocalizations.Bind(["status", "Transfusion", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "Transfusion", "description"]).Localize
		});
		TransfusingStatus = Helper.Content.Statuses.RegisterStatus("Transfusing", new()
		{
			Definition = new()
			{
				icon = Helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Status/Transfusing.png")).Sprite,
				color = new("267F00")
			},
			Name = this.AnyLocalizations.Bind(["status", "Transfusing", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "Transfusing", "description"]).Localize
		});

		foreach (var cardType in AllCardTypes)
			if (Activator.CreateInstance(cardType) is IDraculaCard registerable)
				registerable.Register(helper);

		Helper.Content.Characters.RegisterCharacter("Dracula", new()
		{
			Deck = DraculaDeck.Deck,
			Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
			BorderSprite = StableSpr.panels_enemy_nodeck,
			StarterCardTypes = StarterCardTypes,
			NeutralAnimation = new()
			{
				Deck = DraculaDeck.Deck,
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
				Deck = DraculaDeck.Deck,
				LoopTag = "mini",
				Frames = [
					helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/Character/Mini/0.png")).Sprite
				]
			}
		});
	}
}
