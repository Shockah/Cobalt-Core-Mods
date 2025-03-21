using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.Proxy;

public sealed class ModEntry : SimpleMod
{
	private const int ProxyCount = 3;
	
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly IHarmony Harmony;
	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal readonly List<IDeckEntry> ProxyDecks = [];

	internal static readonly IEnumerable<Type> RegisterableTypes = [
	];

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		Harmony = helper.Utilities.Harmony;
		// KokoroApi = helper.ModRegistry.GetApi<IKokoroApi>("Shockah.Kokoro")!.V2;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);

		for (var i = 0; i < ProxyCount; i++)
			RegisterProxy();

		foreach (var type in RegisterableTypes)
			AccessTools.DeclaredMethod(type, nameof(IRegisterable.Register))?.Invoke(null, [package, helper]);

		void RegisterProxy()
		{
			var index = ProxyDecks.Count;
			var deck = helper.Content.Decks.RegisterDeck($"Proxy{index + 1}", new()
			{
				Definition = new() { color = new("7F7F7F"), titleColor = Colors.black },
				DefaultCardArt = StableSpr.cards_colorless,
				BorderSprite = StableSpr.cardShared_border_colorless,
				Name = this.AnyLocalizations.Bind(["character", "name"]).Localize
			});
			ProxyDecks.Add(deck);
			
			helper.Content.Characters.V2.RegisterPlayableCharacter($"Proxy{index + 1}", new()
			{
				Deck = deck.Deck,
				Description = this.AnyLocalizations.Bind(["character", "description"]).Localize,
				BorderSprite = StableSpr.panels_char,
				NeutralAnimation = new()
				{
					CharacterType = deck.UniqueName,
					LoopTag = "neutral",
					Frames = [StableSpr.panels_char_nodeck]
				},
				MiniAnimation = new()
				{
					CharacterType = deck.UniqueName,
					LoopTag = "mini",
					Frames = [StableSpr.panels_char_mini]
				},
				Starters = new(),
			});
		}
	}
}
