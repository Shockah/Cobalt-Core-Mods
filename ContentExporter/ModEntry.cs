using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Nickel;

namespace Shockah.ContentExporter;

internal sealed partial class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	private readonly Queue<Action<G>> QueuedTasks = new();
	internal readonly CardRenderer CardRenderer = new();
	internal readonly ArtifactTooltipRenderer ArtifactTooltipRenderer;
	internal readonly CardTooltipRenderer CardTooltipRenderer = new();
	internal readonly ShipRenderer ShipRenderer = new();
	
	private static readonly Dictionary<Deck, string> DeckNiceNames = [];

	internal Settings Settings { get; private set; }

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		ArtifactTooltipRenderer = new(helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/ArtifactGlow.png")));

		this.Settings = helper.Storage.LoadJson<Settings>(helper.Storage.GetMainStorageFile("json"));

		var harmony = new Harmony(package.Manifest.UniqueName);
		CardPatches.Apply(harmony);
		EditorPatches.Apply(harmony);
		GPatches.Apply(harmony);
	}

	internal void QueueTask(Action<G> task)
		=> QueuedTasks.Enqueue(task);

	internal void RunNextTask(G g)
	{
		if (!QueuedTasks.TryDequeue(out var task))
			return;
		task(g);

		if (QueuedTasks.Count == 0)
			Logger.LogInformation("Finished all tasks.");
		else if (QueuedTasks.Count % 25 == 0)
			Logger.LogInformation("Tasks left in the queue: {TaskCount}", QueuedTasks.Count);
	}

	internal void QueueSelectedDecksExportTask(G g, bool withScreenFilter)
	{
		if (g.metaRoute?.subRoute is Codex { subRoute: CardBrowse cardCodex })
		{
			foreach (var deck in cardCodex.GetCardList(g).Select(c => c.GetMeta().deck).ToHashSet())
				QueueDeckCardsExportTask(g, withScreenFilter, deck);
		}
		else if (g.state.IsOutsideRun())
		{
			foreach (var deck in g.state.runConfig.selectedChars)
				QueueDeckCardsExportTask(g, withScreenFilter, deck);
		}
		else
		{
			foreach (var character in g.state.characters)
				if (character.deckType is { } deck)
					QueueDeckCardsExportTask(g, withScreenFilter, deck);
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

	internal void QueueDeckCardsExportTask(G g, bool withScreenFilter, Deck deck)
	{
		if (Helper.Content.Decks.LookupByDeck(deck) is { } entry)
			QueueDeckCardsExportTask(g, withScreenFilter, entry);
	}

	internal void QueueDeckCardsExportTask(G g, bool withScreenFilter, IDeckEntry entry)
	{
		if (Settings is { ExportCard: false, ExportCardTooltip: false })
			return;
		
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;

		var cards = DB.cards.Keys
			.Select(key => Helper.Content.Cards.LookupByUniqueName(key))
			.OfType<ICardEntry>()
			.Where(e => e.Configuration.Meta.deck == entry.Deck)
			.Where(e => DB.currentLocale.strings.ContainsKey($"card.{e.UniqueName}.name"))
			.ToList();

		if (cards.Count == 0)
			return;

		var exportableCardsDataPath = Path.Combine(modloaderFolder, "ContentExport", "Cards");
		var exportableTooltipsDataPath = Path.Combine(modloaderFolder, "ContentExport", "CardTooltips");
		var fileSafeDeckName = MakeFileSafe(ObtainDeckNiceName(entry.Deck));
		var cardDeckExportPath = Path.Combine(exportableCardsDataPath, fileSafeDeckName);
		var tooltipDeckExportPath = Path.Combine(exportableTooltipsDataPath, fileSafeDeckName);

		foreach (var e in cards)
		{
			var card = (Card)Activator.CreateInstance(e.Configuration.CardType)!;
			foreach (var upgrade in new List<Upgrade> { Upgrade.None }.Concat(e.Configuration.Meta.upgradesTo))
			{
				var cardAtUpgrade = Mutil.DeepCopy(card);
				cardAtUpgrade.upgrade = upgrade;

				var fileSafeName = MakeFileSafe(cardAtUpgrade.GetFullDisplayName());
				var imagePath = e.Configuration.Meta.unreleased
					? $"Unreleased/{fileSafeName}.png"
					: (
						e.Configuration.Meta.dontOffer
							? $"Unoffered/{fileSafeName}.png"
							: $"{fileSafeName}.png"
					);

				QueueTask(g => CardBaseExportTask(g, withScreenFilter, cardAtUpgrade, Path.Combine(cardDeckExportPath, imagePath)));
				QueueTask(g => CardTooltipExportTask(g, withScreenFilter, cardAtUpgrade, withTheCard: true, Path.Combine(tooltipDeckExportPath, imagePath)));
			}
		}
	}

	internal void QueueAllArtifactsExportTask(G g, bool withScreenFilter)
	{
		foreach (var deck in DB.artifactMetas.Values.Select(meta => meta.owner).ToHashSet())
			QueueDeckArtifactsExportTask(g, withScreenFilter, deck);
	}

	internal void QueueAllShipsExportTask(bool withScreenFilter)
	{
		foreach (var key in StarterShip.ships.Keys)
			if (Helper.Content.Ships.LookupByUniqueName(key) is { } entry)
				QueueShipExportTask(withScreenFilter, entry);
	}

	internal void QueueDeckArtifactsExportTask(G g, bool withScreenFilter, Deck deck)
	{
		if (Helper.Content.Decks.LookupByDeck(deck) is { } entry)
			QueueDeckArtifactsExportTask(g, withScreenFilter, entry);
	}

	internal void QueueDeckArtifactsExportTask(G g, bool withScreenFilter, IDeckEntry entry)
	{
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;

		var artifacts = DB.artifacts.Keys
			.Select(key => Helper.Content.Artifacts.LookupByUniqueName(key))
			.OfType<IArtifactEntry>()
			.Where(e => e.Configuration.Meta.owner == entry.Deck)
			.Where(e => DB.currentLocale.strings.ContainsKey($"artifact.{e.UniqueName}.name"))
			.ToList();

		if (artifacts.Count == 0)
			return;
		
		var exportableDataPath = Path.Combine(modloaderFolder, "ContentExport", "Artifacts");
		var fileSafeDeckName = MakeFileSafe(entry.Deck switch
		{
			Deck.colorless => "_Generic",
			Deck.catartifact => ObtainDeckNiceName(Deck.colorless),
			_ => ObtainDeckNiceName(entry.Deck),
		});
		var deckExportPath = Path.Combine(exportableDataPath, fileSafeDeckName);

		foreach (var e in artifacts)
		{
			var artifact = (Artifact)Activator.CreateInstance(e.Configuration.ArtifactType)!;
			var fileSafeName = MakeFileSafe(e.Configuration.Name?.Invoke(DB.currentLocale.locale) ?? e.UniqueName);
			var tooltipImagePath = e.Configuration.Meta.pools.Contains(ArtifactPool.Unreleased)
				? $"Unreleased/{fileSafeName}.png"
				: $"{fileSafeName}.png";
			QueueTask(g => ArtifactExportTask(g, withScreenFilter, artifact, Path.Combine(deckExportPath, tooltipImagePath)));
		}
	}

	internal void QueueShipExportTask(bool withScreenFilter, IShipEntry entry)
	{
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;
		var exportableDataPath = Path.Combine(modloaderFolder, "ContentExport", "Ships");
		Directory.CreateDirectory(exportableDataPath);
		
		var fileSafeName = MakeFileSafe(entry.Configuration.Name?.Invoke(DB.currentLocale.locale) ?? entry.UniqueName);
		var imagePath = Path.Combine(exportableDataPath, $"{fileSafeName}.png");
		QueueTask(g => ShipExportTask(g, withScreenFilter, entry.Configuration.Ship.ship, imagePath));
	}

	private void CardBaseExportTask(G g, bool withScreenFilter, Card card, string path)
	{
		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		CardRenderer.Render(g, withScreenFilter, card, stream);
	}

	private void CardTooltipExportTask(G g, bool withScreenFilter, Card card, bool withTheCard, string path)
	{
		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		CardTooltipRenderer.Render(g, withScreenFilter, card, withTheCard, stream);
	}

	private void ArtifactExportTask(G g, bool withScreenFilter, Artifact artifact, string path)
	{
		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		ArtifactTooltipRenderer.Render(g, withScreenFilter, artifact, stream);
	}

	private void ShipExportTask(G g, bool withScreenFilter, Ship ship, string path)
	{
		Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
		using var stream = new FileStream(path, FileMode.Create);
		ShipRenderer.Render(g, withScreenFilter, ship, stream);
	}

	[GeneratedRegex("\\w+$")]
	private static partial Regex LastWordRegex();
}
