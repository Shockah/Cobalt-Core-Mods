using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;
using Nickel.ModSettings;

namespace Shockah.ContentExporter;

internal sealed partial class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	internal readonly ILocalizationProvider<IReadOnlyList<string>> AnyLocalizations;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal readonly Queue<Action<G>> QueuedTasks = new();
	private readonly CardRenderer CardRenderer = new();
	private readonly CardTooltipRenderer CardTooltipRenderer = new();
	private readonly CardUpgradesRenderer CardUpgradesRenderer = new();
	private readonly ArtifactTooltipRenderer ArtifactTooltipRenderer = new();
	private readonly ShipRenderer ShipRenderer = new();
	private readonly ShipDescriptionRenderer ShipDescriptionRenderer = new();
	
	internal readonly ISpriteEntry PremultipliedGlowSprite;
	internal readonly ISpriteEntry BossArtifactGlowSprite;
	
	private static readonly Dictionary<Deck, string> DeckNiceNames = [];

	internal Settings Settings { get; private set; }

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		var modSettingsApi = helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings")!;

		this.AnyLocalizations = new JsonLocalizationProvider(
			tokenExtractor: new SimpleLocalizationTokenExtractor(),
			localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
		);
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(this.AnyLocalizations)
		);
		
		PremultipliedGlowSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/PremultipliedGlow.png"));
		BossArtifactGlowSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/BossArtifactGlow.png"));

		this.Settings = helper.Storage.LoadJson<Settings>(helper.Storage.GetMainStorageFile("json"));

		var harmony = new Harmony(package.Manifest.UniqueName);
		CardPatches.Apply(harmony);
		DrawPatches.Apply(harmony);
		GPatches.Apply(harmony);
		SharedArtPatches.Apply(harmony);
		TutorialPatches.Apply(harmony);
		
		modSettingsApi.RegisterModSettings(modSettingsApi.MakeList([
			modSettingsApi.MakeConditional(
				modSettingsApi.MakeButton(
					() => Localizations.Localize(["settings", "export", "title"]),
					(g, _) =>
					{
						QueueCardExportTasks(g);
						QueueArtifactExportTasks(g);
						QueueShipExportTasks(g);
					}
				),
				() => QueuedTasks.Count == 0
			),
			modSettingsApi.MakeConditional(
				modSettingsApi.MakeButton(
					() => Localizations.Localize(["settings", "export", "cancel"]),
					(_, _) => QueuedTasks.Clear()
				),
				() => QueuedTasks.Count != 0
			),
			modSettingsApi.MakeEnumStepper(
				title: () => Localizations.Localize(["settings", "background", "title"]),
				getter: () => Settings.Background,
				setter: value => Settings.Background = value
			).SetValueFormatter(
				value => Localizations.Localize(["settings", "background", "value", value.ToString()])
			).SetValueWidth(
				_ => 100
			),
			modSettingsApi.MakeCheckbox(
				() => Localizations.Localize(["settings", "screenFilter", "title"]),
				() => Settings.ScreenFilter,
				(_, _, value) => Settings.ScreenFilter = value
			),
			
			modSettingsApi.MakeCheckbox(
				() => Localizations.Localize(["settings", "exportCards", "title"]),
				() => Settings.CardsScale is not null,
				(_, _, value) => Settings.CardsScale = value ? Settings.DEFAULT_SCALE : null 
			),
			modSettingsApi.MakeConditional(
				modSettingsApi.MakeNumericStepper(
					() => Localizations.Localize(["settings", "scaleSubsetting", "title"]),
					() => Settings.CardsScale ?? 0,
					value => Settings.CardsScale = value,
					minValue: 1, maxValue: 8
				),
				() => Settings.CardsScale is not null
			),
			
			modSettingsApi.MakeCheckbox(
				() => Localizations.Localize(["settings", "exportCardTooltips", "title"]),
				() => Settings.CardTooltipsScale is not null,
				(_, _, value) => Settings.CardTooltipsScale = value ? Settings.DEFAULT_SCALE : null 
			),
			modSettingsApi.MakeConditional(
				modSettingsApi.MakeNumericStepper(
					() => Localizations.Localize(["settings", "scaleSubsetting", "title"]),
					() => Settings.CardTooltipsScale ?? 0,
					value => Settings.CardTooltipsScale = value,
					minValue: 1, maxValue: 8
				),
				() => Settings.CardTooltipsScale is not null
			),
			
			modSettingsApi.MakeCheckbox(
				() => Localizations.Localize(["settings", "exportCardUpgrades", "title"]),
				() => Settings.CardUpgradesScale is not null,
				(_, _, value) => Settings.CardUpgradesScale = value ? Settings.DEFAULT_SCALE : null 
			),
			modSettingsApi.MakeConditional(
				modSettingsApi.MakeNumericStepper(
					() => Localizations.Localize(["settings", "scaleSubsetting", "title"]),
					() => Settings.CardUpgradesScale ?? 0,
					value => Settings.CardUpgradesScale = value,
					minValue: 1, maxValue: 8
				),
				() => Settings.CardUpgradesScale is not null
			),
			
			modSettingsApi.MakeCheckbox(
				() => Localizations.Localize(["settings", "exportArtifacts", "title"]),
				() => Settings.ArtifactsScale is not null,
				(_, _, value) => Settings.ArtifactsScale = value ? Settings.DEFAULT_SCALE : null 
			),
			modSettingsApi.MakeConditional(
				modSettingsApi.MakeNumericStepper(
					() => Localizations.Localize(["settings", "scaleSubsetting", "title"]),
					() => Settings.ArtifactsScale ?? 0,
					value => Settings.ArtifactsScale = value,
					minValue: 1, maxValue: 8
				),
				() => Settings.ArtifactsScale is not null
			),
			
			modSettingsApi.MakeCheckbox(
				() => Localizations.Localize(["settings", "exportShips", "title"]),
				() => Settings.ShipsScale is not null,
				(_, _, value) => Settings.ShipsScale = value ? Settings.DEFAULT_SCALE : null 
			),
			modSettingsApi.MakeConditional(
				modSettingsApi.MakeNumericStepper(
					() => Localizations.Localize(["settings", "scaleSubsetting", "title"]),
					() => Settings.ShipsScale ?? 0,
					value => Settings.ShipsScale = value,
					minValue: 1, maxValue: 8
				),
				() => Settings.ShipsScale is not null
			),
			
			modSettingsApi.MakeCheckbox(
				() => Localizations.Localize(["settings", "exportShipDescriptions", "title"]),
				() => Settings.ShipDescriptionsScale is not null,
				(_, _, value) => Settings.ShipDescriptionsScale = value ? Settings.DEFAULT_SCALE : null 
			),
			modSettingsApi.MakeConditional(
				modSettingsApi.MakeNumericStepper(
					() => Localizations.Localize(["settings", "scaleSubsetting", "title"]),
					() => Settings.ShipDescriptionsScale ?? 0,
					value => Settings.ShipDescriptionsScale = value,
					minValue: 1, maxValue: 8
				),
				() => Settings.ShipDescriptionsScale is not null
			),
			
			modSettingsApi.MakeButton(
				() => Localizations.Localize(["settings", "filterToMods", "title"]),
				(g, route) => route.OpenSubroute(g, modSettingsApi.MakeModSettingsRoute(modSettingsApi.MakeList([
					modSettingsApi.MakeHeader(
						() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
						() => Localizations.Localize(["settings", "filterToMods", "title"])
					),
					modSettingsApi.MakeList(
						helper.ModRegistry.LoadedMods.Values
							.Where(mod =>
							{
								var modHelper = helper.ModRegistry.GetModHelper(mod);
								return modHelper.Content.Cards.RegisteredCards.Any() || modHelper.Content.Artifacts.RegisteredArtifacts.Any() || modHelper.Content.Ships.RegisteredShips.Any();
							})
							.OrderBy(mod => mod.DisplayName ?? mod.UniqueName)
							.Prepend(helper.ModRegistry.VanillaModManifest)
							.Select(IModSettingsApi.IModSetting (mod) => modSettingsApi.MakeCheckbox(
								() => mod.DisplayName ?? mod.UniqueName,
								() => Settings.FilterToMods.Contains(mod.UniqueName),
								(_, _, value) =>
								{
									if (value)
										Settings.FilterToMods.Add(mod.UniqueName);
									else
										Settings.FilterToMods.Remove(mod.UniqueName);
								}
							).SetTitleFont(() => DB.pinch).SetHeight(17))
							.ToList()
					),
					modSettingsApi.MakeBackButton()
				])))
			).SetValueText(() =>
			{
				if (Settings.FilterToMods.Count == 0)
					return Localizations.Localize(["settings", "filterToMods", "none"]);
				if (Settings.FilterToMods.Count == 1)
				{
					if (Settings.FilterToMods.First() == helper.ModRegistry.VanillaModManifest.UniqueName)
						return "Cobalt Core";
					return helper.ModRegistry.LoadedMods.GetValueOrDefault(Settings.FilterToMods.First())?.DisplayName ?? Settings.FilterToMods.First();
				}
				return Localizations.Localize(["settings", "filterToMods", "count"], new { Count = Settings.FilterToMods.Count });
			}).SetValueTextFont(() => DB.pinch),
			modSettingsApi.MakeCheckbox(
				() => Localizations.Localize(["settings", "filterToRun", "title"]),
				() => Settings.FilterToRun,
				(_, _, value) => Settings.FilterToRun = value
			),
		]).SubscribeToOnMenuClose(_ =>
		{
			helper.Storage.SaveJson(helper.Storage.GetMainStorageFile("json"), Settings);
		}));
	}

	internal void QueueTask(Action<G> task)
		=> QueuedTasks.Enqueue(task);

	internal void RunTasksReasonably(G g)
	{
		if (QueuedTasks.Count == 0)
			return;

		var stopwatch = Stopwatch.StartNew();
		while (QueuedTasks.TryDequeue(out var task))
		{
			task(g);
			if (stopwatch.ElapsedMilliseconds >= 1000.0 / 60.0 * 0.5) // up to 50% of a single 60FPS frame's budget
				break;
		}
	}

	private static string ObtainDeckNiceName(Deck deck)
	{
		ref var niceName = ref CollectionsMarshal.GetValueRefOrAddDefault(DeckNiceNames, deck, out var niceNameExists);
		if (!niceNameExists)
		{
			niceName = GetNiceName(deck);
			
			static string GetNiceName(Deck deck)
			{
				var locKey = $"char.{deck.Key()}";
				if (DB.currentLocale.strings.TryGetValue(locKey, out var localized))
					return localized;

				var match = LastWordRegex().Match(deck.Key());
				if (match.Success)
				{
					var text = match.Value;
					if (text.Length >= 1)
						text = string.Concat(text[0].ToString().ToUpper(), text.AsSpan(1));
					return text;
				}

				return ((int)deck).ToString();
			}
		}
		return niceName!;
	}

	private static string MakeFileSafe(string path)
	{
		foreach (var unsafeChar in Path.GetInvalidFileNameChars())
			path = path.Replace(unsafeChar, '_');
		return path;
	}

	private void QueueCardExportTasks(G g)
	{
		var cardsScale = Math.Max(Settings.CardsScale ?? 0, 0);
		var cardTooltipsScale = Math.Max(Settings.CardTooltipsScale ?? 0, 0);
		var cardUpgradesScale = Math.Max(Settings.CardUpgradesScale ?? 0, 0);
		if (cardsScale <= 0 && cardTooltipsScale <= 0 && cardUpgradesScale <= 0)
			return;
		
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;
		var screenFilter = Settings.ScreenFilter;
		var background = Settings.Background;

		var isOutsideRun = g.state.IsOutsideRun();
		var uniqueNameCounts = new Dictionary<DeckAndString, int>();
		var cards = DB.cards.Keys
			.Select(key => Helper.Content.Cards.LookupByUniqueName(key))
			.OfType<ICardEntry>()
			.Where(e =>
			{
				if (Settings.FilterToRun)
				{
					if (isOutsideRun)
					{
						if (!g.state.runConfig.selectedChars.Contains(e.Configuration.Meta.deck))
							return false;
					}
					else
					{
						if (g.state.characters.All(character => character.deckType != e.Configuration.Meta.deck))
							return false;
					}
				}
				if (Settings.FilterToMods.Count != 0 && !Settings.FilterToMods.Contains(e.ModOwner.UniqueName))
					return false;
				return true;
			})
			.Where(e => DB.currentLocale.strings.ContainsKey($"card.{e.UniqueName}.name"))
			.Select(e =>
			{
				var uniqueNameCountKey = new DeckAndString(e.Configuration.Meta.deck, Loc.T($"card.{e.UniqueName}.name"));
				uniqueNameCounts[uniqueNameCountKey] = uniqueNameCounts.GetValueOrDefault(uniqueNameCountKey) + 1;
				return e;
			})
			.ToList();

		if (cards.Count == 0)
			return;

		var exportableCardsDataPath = Path.Combine(modloaderFolder, "ContentExport", "Cards");
		var exportableTooltipsDataPath = Path.Combine(modloaderFolder, "ContentExport", "CardTooltips");
		var exportableUpgradesDataPath = Path.Combine(modloaderFolder, "ContentExport", "CardUpgrades");

		foreach (var e in cards)
		{
			var fileSafeDeckName = MakeFileSafe(ObtainDeckNiceName(e.Configuration.Meta.deck));
			var cardDeckExportPath = Path.Combine(exportableCardsDataPath, fileSafeDeckName);
			var tooltipDeckExportPath = Path.Combine(exportableTooltipsDataPath, fileSafeDeckName);
			var upgradesExportPath = Path.Combine(exportableUpgradesDataPath, fileSafeDeckName);
			
			var card = (Card)Activator.CreateInstance(e.Configuration.CardType)!;
			foreach (var upgrade in new List<Upgrade> { Upgrade.None }.Concat(e.Configuration.Meta.upgradesTo))
			{
				var cardAtUpgrade = Mutil.DeepCopy(card);
				cardAtUpgrade.upgrade = upgrade;

				string fileName;
				var uniqueNameCountKey = new DeckAndString(e.Configuration.Meta.deck, Loc.T($"card.{e.UniqueName}.name"));
				if (uniqueNameCounts.GetValueOrDefault(uniqueNameCountKey) >= 2)
				{
					fileName = e.UniqueName;
					if (upgrade != Upgrade.None)
					{
						var baseName = card.GetFullDisplayName();
						var upgradedName = cardAtUpgrade.GetFullDisplayName();
						fileName = $"{fileName}{upgradedName[baseName.Length..]}";
					}
				}
				else
				{
					fileName = cardAtUpgrade.GetFullDisplayName();
				}

				var fileSafeName = MakeFileSafe(fileName);
				var imagePath = e.Configuration.Meta.unreleased
					? $"Unreleased/{fileSafeName}.png"
					: (
						e.Configuration.Meta.dontOffer
							? $"Unoffered/{fileSafeName}.png"
							: $"{fileSafeName}.png"
					);

				QueueTask(g => CardBaseExportTask(g, cardsScale, screenFilter, background, cardAtUpgrade, Path.Combine(cardDeckExportPath, imagePath)));
				QueueTask(g => CardTooltipExportTask(g, cardTooltipsScale, screenFilter, background, cardAtUpgrade, withTheCard: true, Path.Combine(tooltipDeckExportPath, imagePath)));
			}

			{
				var uniqueNameCountKey = new DeckAndString(e.Configuration.Meta.deck, Loc.T($"card.{e.UniqueName}.name"));
				var fileName = uniqueNameCounts.GetValueOrDefault(uniqueNameCountKey) >= 2 ? e.UniqueName : card.GetFullDisplayName();
				var fileSafeName = MakeFileSafe(fileName);
				var imagePath = e.Configuration.Meta.unreleased
					? $"Unreleased/{fileSafeName}.png"
					: (
						e.Configuration.Meta.dontOffer
							? $"Unoffered/{fileSafeName}.png"
							: $"{fileSafeName}.png"
					);
				
				QueueTask(g => CardUpgradesExportTask(g, cardUpgradesScale, screenFilter, background, card, Path.Combine(upgradesExportPath, imagePath)));
			}
		}
	}

	private void QueueShipExportTasks(G g)
	{
		var shipsScale = Math.Max(Settings.ShipsScale ?? 0, 0);
		var shipDescriptionsScale = Math.Max(Settings.ShipDescriptionsScale ?? 0, 0);
		if (shipsScale <= 0 && shipDescriptionsScale <= 0)
			return;
		
		var isOutsideRun = g.state.IsOutsideRun();
		
		foreach (var key in StarterShip.ships.Keys)
		{
			if (Helper.Content.Ships.LookupByUniqueName(key) is not { } entry)
				continue;
			if (Settings.FilterToRun)
			{
				if (isOutsideRun)
				{
					if (g.state.runConfig.selectedShip != key)
						continue;
				}
				else
				{
					if (g.state.ship.key != key)
						continue;
				}
			}
			
			if (Settings.FilterToRun && !isOutsideRun && g.state.ship.key != key)
				continue;
			if (Settings.FilterToMods.Count != 0 && !Settings.FilterToMods.Contains(entry.ModOwner.UniqueName))
				continue;
			
			QueueShipExportTask(shipsScale, Settings.ScreenFilter, Settings.Background, entry);
			QueueShipDescriptionExportTask(shipDescriptionsScale, Settings.ScreenFilter, Settings.Background, entry);
		}
	}

	private void QueueArtifactExportTasks(G g)
	{
		if (Settings.ArtifactsScale is not { } scale || scale <= 0)
			return;
		
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;
		var screenFilter = Settings.ScreenFilter;
		var background = Settings.Background;

		var isOutsideRun = g.state.IsOutsideRun();
		var artifacts = DB.artifacts.Keys
			.Select(key => Helper.Content.Artifacts.LookupByUniqueName(key))
			.OfType<IArtifactEntry>()
			.Where(e =>
			{
				if (Settings.FilterToRun && e.Configuration.Meta.owner != Deck.colorless)
				{
					var testedDeck = e.Configuration.Meta.owner == Deck.catartifact ? Deck.colorless : e.Configuration.Meta.owner;
					
					if (isOutsideRun)
					{
						if (g.state.runConfig.selectedChars.Contains(testedDeck))
							return false;
					}
					else
					{
						if (g.state.characters.All(character => character.deckType != testedDeck))
							return false;
					}
				}
				if (Settings.FilterToMods.Count != 0 && !Settings.FilterToMods.Contains(e.ModOwner.UniqueName))
					return false;
				return true;
			})
			.Where(e => DB.currentLocale.strings.ContainsKey($"artifact.{e.UniqueName}.name"))
			.ToList();

		if (artifacts.Count == 0)
			return;
		
		var exportableDataPath = Path.Combine(modloaderFolder, "ContentExport", "Artifacts");
		var dailyExportPath = Path.Combine(exportableDataPath, MakeFileSafe("_Daily"));

		foreach (var e in artifacts)
		{
			var fileSafeDeckName = MakeFileSafe(e.Configuration.Meta.owner switch
			{
				Deck.colorless => "_Generic",
				Deck.catartifact => ObtainDeckNiceName(Deck.colorless),
				_ => ObtainDeckNiceName(e.Configuration.Meta.owner),
			});
			var deckExportPath = Path.Combine(exportableDataPath, fileSafeDeckName);
			
			var artifact = (Artifact)Activator.CreateInstance(e.Configuration.ArtifactType)!;
			var fileSafeName = MakeFileSafe(e.Configuration.Name?.Invoke(DB.currentLocale.locale) ?? e.UniqueName);
			var tooltipImagePath = e.Configuration.Meta.pools.Contains(ArtifactPool.Unreleased)
				? $"Unreleased/{fileSafeName}.png"
				: $"{fileSafeName}.png";
			var finalPath = Path.Combine(
				e.Configuration.Meta.pools.Contains(ArtifactPool.DailyOnly) ? dailyExportPath : deckExportPath,
				tooltipImagePath
			);
			QueueTask(g => ArtifactExportTask(g, scale, screenFilter, background, artifact, finalPath));
		}
	}

	private void QueueShipExportTask(int scale, bool withScreenFilter, ExportBackground background, IShipEntry entry)
	{
		if (scale <= 0)
			return;

		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;
		var exportableDataPath = Path.Combine(modloaderFolder, "ContentExport", "Ships");
		Directory.CreateDirectory(exportableDataPath);
		
		var fileSafeName = MakeFileSafe(entry.Configuration.Name?.Invoke(DB.currentLocale.locale) ?? entry.UniqueName);
		var imagePath = Path.Combine(exportableDataPath, $"{fileSafeName}.png");
		QueueTask(g => ShipExportTask(g, scale, withScreenFilter, background, entry.Configuration.Ship.ship, imagePath));
	}

	private void QueueShipDescriptionExportTask(int scale, bool withScreenFilter, ExportBackground background, IShipEntry entry)
	{
		if (scale <= 0)
			return;

		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;
		var exportableDataPath = Path.Combine(modloaderFolder, "ContentExport", "ShipDescriptions");
		Directory.CreateDirectory(exportableDataPath);
		
		var fileSafeName = MakeFileSafe(entry.Configuration.Name?.Invoke(DB.currentLocale.locale) ?? entry.UniqueName);
		var imagePath = Path.Combine(exportableDataPath, $"{fileSafeName}.png");
		QueueTask(g => ShipDescriptionExportTask(g, scale, withScreenFilter, background, entry.Configuration.Ship.ship, imagePath));
	}

	private void CardBaseExportTask(G g, int scale, bool withScreenFilter, ExportBackground background, Card card, string path)
	{
		if (scale <= 0)
			return;

		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		CardRenderer.Render(g, scale, withScreenFilter, background, card, stream);
	}

	private void CardTooltipExportTask(G g, int scale, bool withScreenFilter, ExportBackground background, Card card, bool withTheCard, string path)
	{
		if (scale <= 0)
			return;

		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		CardTooltipRenderer.Render(g, scale, withScreenFilter, background, card, withTheCard, stream);
	}

	private void CardUpgradesExportTask(G g, int scale, bool withScreenFilter, ExportBackground background, Card card, string path)
	{
		if (scale <= 0)
			return;

		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		CardUpgradesRenderer.Render(g, scale, withScreenFilter, background, card, stream);
	}

	private void ArtifactExportTask(G g, int scale, bool withScreenFilter, ExportBackground background, Artifact artifact, string path)
	{
		if (scale <= 0)
			return;
		
		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		ArtifactTooltipRenderer.Render(g, scale, withScreenFilter, background, artifact, stream);
	}

	private void ShipExportTask(G g, int scale, bool withScreenFilter, ExportBackground background, Ship ship, string path)
	{
		if (scale <= 0)
			return;
		
		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		ShipRenderer.Render(g, scale, withScreenFilter, background, ship, stream);
	}

	private void ShipDescriptionExportTask(G g, int scale, bool withScreenFilter, ExportBackground background, Ship ship, string path)
	{
		if (scale <= 0)
			return;
		
		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		ShipDescriptionRenderer.Render(g, scale, withScreenFilter, background, ship, stream);
	}

	[GeneratedRegex("\\w+$")]
	private static partial Regex LastWordRegex();
}
