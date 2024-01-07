using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Dracula;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	internal IKokoroApi KokoroApi { get; }
	internal ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations { get; }
	internal ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations { get; }

	internal IDeckEntry DraculaDeck { get; }
	internal IStatusEntry BleedingStatus { get; }

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
	];

	internal static IEnumerable<Type> AllCardTypes
		=> StarterCardTypes.Concat(CommonCardTypes);

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!;
		KokoroApi.RegisterStatusLogicHook(new BleedingManager(), 0);

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

		ShieldCostOff = Helper.Content.Sprites.RegisterSprite(() => package.PackageRoot.GetRelativeFile("assets/Icons/ShieldCostOff.png").OpenRead());
		ShieldCostOn = Helper.Content.Sprites.RegisterSprite(() => package.PackageRoot.GetRelativeFile("assets/Icons/ShieldCostOn.png").OpenRead());
		BleedingCostOff = Helper.Content.Sprites.RegisterSprite(() => package.PackageRoot.GetRelativeFile("assets/Icons/BleedingCostOff.png").OpenRead());
		BleedingCostOn = Helper.Content.Sprites.RegisterSprite(() => package.PackageRoot.GetRelativeFile("assets/Icons/BleedingCostOn.png").OpenRead());

		BleedingStatus = Helper.Content.Statuses.RegisterStatus("Bleeding", new()
		{
			Definition = new()
			{
				icon = Helper.Content.Sprites.RegisterSprite(() => package.PackageRoot.GetRelativeFile("assets/Status/Bleeding.png").OpenRead()).Sprite,
				color = new("BE0000")
			},
			Name = this.AnyLocalizations.Bind(["status", "Bleeding", "name"]).Localize,
			Description = this.AnyLocalizations.Bind(["status", "Bleeding", "description"]).Localize
		});

		foreach (var cardType in AllCardTypes)
			if (Activator.CreateInstance(cardType) is IRegisterableCard registerable)
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
					helper.Content.Sprites.RegisterSprite(() => package.PackageRoot.GetRelativeFile("assets/Character/Mini/0.png").OpenRead()).Sprite
				]
			}
		});
	}
}
