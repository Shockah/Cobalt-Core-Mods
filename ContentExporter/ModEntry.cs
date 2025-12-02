using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
using Shockah.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.ContentExporter;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	private readonly Queue<Action<G>> QueuedTasks = new();
	internal readonly CardRenderer CardRenderer = new();
	internal readonly ArtifactTooltipRenderer ArtifactTooltipRenderer;
	internal readonly CardTooltipRenderer CardTooltipRenderer = new();
	internal readonly ShipRenderer ShipRenderer = new();

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

	internal void QueueDeckCardsExportTask(G g, bool withScreenFilter, Deck deck)
	{
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;

		var cards = DB.cards
			.Select(kvp => (Key: kvp.Key, Type: kvp.Value, Meta: DB.cardMetas.GetValueOrDefault(kvp.Key)))
			.Where(e => e.Meta is not null)
			.Select(e => (Key: e.Key, Type: e.Type, Meta: e.Meta!))
			.Where(e => e.Meta.deck == deck)
			.Where(e => DB.currentLocale.strings.ContainsKey($"card.{e.Key}.name"))
			.ToList();

		var exportableData = new ExportDeckCardData(
			Key: deck.Key(),
			Name: Loc.T($"char.{deck.Key()}"),
			PlayableCharacter: NewRunOptions.allChars.Contains(deck),
			Mod: Enum.GetValues<Deck>().Contains(deck) ? null : GetModName(Helper.Content.Decks.LookupByDeck(deck)),
			Cards: cards.Select(e =>
			{
				var card = (Card)Activator.CreateInstance(e.Type)!;
				return new ExportCardData(
					Key: e.Key,
					Name: Loc.T($"card.{e.Key}.name"),
					Rarity: e.Meta.rarity,
					Released: !e.Meta.unreleased,
					Offered: !e.Meta.dontOffer,
					Mod: e.Type.Assembly == typeof(G).Assembly ? null : GetModName(Helper.Content.Cards.LookupByCardType(e.Type)),
					Upgrades: new List<Upgrade> { Upgrade.None }.Concat(e.Meta.upgradesTo).Select(upgrade =>
					{
						var cardAtUpgrade = Mutil.DeepCopy(card);
						cardAtUpgrade.upgrade = upgrade;
						var data = cardAtUpgrade.GetData(DB.fakeState);
						var traits = Helper.Content.Cards.GetActiveCardTraits(DB.fakeState, cardAtUpgrade);
						return (
							Upgrade: upgrade,
							Model: new ExportCardUpgradeData(
								Description: data.description,
								Cost: data.cost,
								Traits: traits.Select(t =>
								{
									try
									{
										if (t.Configuration.Name is not { } nameProvider || nameProvider("en") is not { } name || string.IsNullOrEmpty(name))
											return null;
										return new ExportCardTraitData(Key: t.UniqueName, Name: name);
									}
									catch
									{
										Logger.LogError("There was an error exporting card trait data for card {Card} {Upgrade}.", cardAtUpgrade.Key(), cardAtUpgrade.upgrade);
										return null;
									}
								}).WhereNotNull().ToList(),
								BaseImagePath: e.Meta.unreleased
									? $"unreleased/{MakeFileSafe(e.Key)}-Base-{(upgrade == Upgrade.None ? "0" : upgrade.ToString())}.png"
									: (
										e.Meta.dontOffer
											? $"unoffered/{MakeFileSafe(e.Key)}-Base-{(upgrade == Upgrade.None ? "0" : upgrade.ToString())}.png"
											: $"{MakeFileSafe(e.Key)}-Base-{(upgrade == Upgrade.None ? "0" : upgrade.ToString())}.png"
									),
								TooltipImagePath: e.Meta.unreleased
									? $"unreleased/{MakeFileSafe(e.Key)}-Tooltip-{(upgrade == Upgrade.None ? "0" : upgrade.ToString())}.png"
									: (
										e.Meta.dontOffer
											? $"unoffered/{MakeFileSafe(e.Key)}-Tooltip-{(upgrade == Upgrade.None ? "0" : upgrade.ToString())}.png"
											: $"{MakeFileSafe(e.Key)}-Tooltip-{(upgrade == Upgrade.None ? "0" : upgrade.ToString())}.png"
									)
							)
						);
					}).ToDictionary(e => e.Upgrade, e => e.Model)
				);
			}).ToList()
		);

		var exportableDataPath = Path.Combine(modloaderFolder, "CatDiscordBotDataExport", "cards");
		Directory.CreateDirectory(exportableDataPath);
		var deckExportPath = Path.Combine(exportableDataPath, MakeFileSafe(deck.Key()));
		Directory.CreateDirectory(deckExportPath);

		File.WriteAllText(Path.Combine(deckExportPath, "data.json"), JsonConvert.SerializeObject(exportableData, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented
		}));

		if (exportableData.Cards.Any(m => !m.Released))
			Directory.CreateDirectory(Path.Combine(deckExportPath, "unreleased"));
		if (exportableData.Cards.Any(m => m is { Released: true, Offered: false }))
			Directory.CreateDirectory(Path.Combine(deckExportPath, "unoffered"));

		foreach (var exportableCard in exportableData.Cards)
		{
			var card = (Card)Activator.CreateInstance(DB.cards.First(e => e.Key == exportableCard.Key).Value)!;
			foreach (var exportableUpgrade in exportableCard.Upgrades)
			{
				var cardAtUpgrade = Mutil.DeepCopy(card);
				cardAtUpgrade.upgrade = exportableUpgrade.Key;

				QueueTask(g => CardBaseExportTask(g, withScreenFilter, cardAtUpgrade, Path.Combine(deckExportPath, exportableUpgrade.Value.BaseImagePath)));
				QueueTask(g => CardTooltipExportTask(g, withScreenFilter, cardAtUpgrade, withTheCard: true, Path.Combine(deckExportPath, exportableUpgrade.Value.TooltipImagePath)));
			}
		}

		static string MakeFileSafe(string path)
		{
			foreach (var unsafeChar in Path.GetInvalidFileNameChars())
				path = path.Replace(unsafeChar, '_');
			return path;
		}

		static string? GetModName(IModOwned? entry)
			=> entry is null ? null : $"{entry.ModOwner.DisplayName ?? entry.ModOwner.UniqueName}{(string.IsNullOrEmpty(entry.ModOwner.Author) ? "" : $" by {entry.ModOwner.Author}")}";
	}

	internal void QueueAllArtifactsExportTask(G g, bool withScreenFilter)
	{
		foreach (var deck in DB.artifactMetas.Values.Select(meta => meta.owner).ToHashSet())
			QueueDeckArtifactsExportTask(g, withScreenFilter, deck);
	}

	internal void QueueAllShipsExportTask(bool withScreenFilter)
	{
		foreach (var ship in StarterShip.ships.Values)
			QueueShipExportTask(withScreenFilter, ship.ship);
	}

	internal void QueueDeckArtifactsExportTask(G g, bool withScreenFilter, Deck deck)
	{
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;

		static string MakeFileSafe(string path)
		{
			foreach (var unsafeChar in Path.GetInvalidFileNameChars())
				path = path.Replace(unsafeChar, '_');
			return path;
		}

		static string? GetModName(IModOwned? entry)
			=> entry is null ? null : $"{entry.ModOwner.DisplayName ?? entry.ModOwner.UniqueName}{(string.IsNullOrEmpty(entry.ModOwner.Author) ? "" : $" by {entry.ModOwner.Author}")}";

		var artifacts = DB.artifacts
			.Select(kvp => (Key: kvp.Key, Type: kvp.Value, Meta: DB.artifactMetas.GetValueOrDefault(kvp.Key)))
			.Where(e => e.Meta is not null)
			.Select(e => (Key: e.Key, Type: e.Type, Meta: e.Meta!))
			.Where(e => e.Meta.owner == deck)
			.Where(e => DB.currentLocale.strings.ContainsKey($"artifact.{e.Key}.name"))
			.ToList();

		var exportableData = new ExportDeckArtifactData(
			Key: deck.Key(),
			Name: Loc.T($"char.{deck.Key()}"),
			PlayableCharacter: NewRunOptions.allChars.Contains(deck),
			Mod: Enum.GetValues<Deck>().Contains(deck) ? null : GetModName(Helper.Content.Decks.LookupByDeck(deck)),
			Artifacts: artifacts.Select(e =>
			{
				// var artifact = (Artifact)Activator.CreateInstance(e.Type)!;
				return new ExportArtifactData(
					Key: e.Key,
					Name: Loc.T($"artifact.{e.Key}.name"),
					Description: Loc.T($"artifact.{e.Key}.desc"),
					Common: e.Meta.pools.Contains(ArtifactPool.Common),
					Boss: e.Meta.pools.Contains(ArtifactPool.Boss),
					EventOnly: e.Meta.pools.Contains(ArtifactPool.EventOnly),
					Released: !e.Meta.pools.Contains(ArtifactPool.Unreleased),
					Removable: !e.Meta.unremovable,
					Mod: e.Type.Assembly == typeof(G).Assembly ? null : GetModName(Helper.Content.Cards.LookupByCardType(e.Type)),
					TooltipImagePath: e.Meta.pools.Contains(ArtifactPool.Unreleased)
						? $"unreleased/{MakeFileSafe(e.Key)}.png"
						: $"{MakeFileSafe(e.Key)}.png"
				);
			}).ToList()
		);

		var exportableDataPath = Path.Combine(modloaderFolder, "CatDiscordBotDataExport", "artifacts");
		Directory.CreateDirectory(exportableDataPath);
		var deckExportPath = Path.Combine(exportableDataPath, MakeFileSafe(deck.Key()));
		Directory.CreateDirectory(deckExportPath);

		File.WriteAllText(Path.Combine(deckExportPath, "data.json"), JsonConvert.SerializeObject(exportableData, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented
		}));

		if (exportableData.Artifacts.Any(m => !m.Released))
			Directory.CreateDirectory(Path.Combine(deckExportPath, "unreleased"));

		foreach (var exportableArtifact in exportableData.Artifacts)
		{
			var artifact = (Artifact)Activator.CreateInstance(DB.artifacts.First(e => e.Key == exportableArtifact.Key).Value)!;
			QueueTask(g => ArtifactExportTask(g, withScreenFilter, artifact, Path.Combine(deckExportPath, exportableArtifact.TooltipImagePath)));
		}
	}

	internal void QueueShipExportTask(bool withScreenFilter, Ship ship)
	{
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;

		static string MakeFileSafe(string path)
		{
			foreach (var unsafeChar in Path.GetInvalidFileNameChars())
				path = path.Replace(unsafeChar, '_');
			return path;
		}

		var exportableDataPath = Path.Combine(modloaderFolder, "CatDiscordBotDataExport", "ships");
		Directory.CreateDirectory(exportableDataPath);
		
		var imagePath = Path.Combine(exportableDataPath, $"{MakeFileSafe(ship.key)}.png");
		QueueTask(g => ShipExportTask(g, withScreenFilter, ship, imagePath));
	}

	private void CardBaseExportTask(G g, bool withScreenFilter, Card card, string path)
	{
		using var stream = new FileStream(path, FileMode.Create);
		CardRenderer.Render(g, withScreenFilter, card, stream);
	}

	private void CardTooltipExportTask(G g, bool withScreenFilter, Card card, bool withTheCard, string path)
	{
		using var stream = new FileStream(path, FileMode.Create);
		CardTooltipRenderer.Render(g, withScreenFilter, card, withTheCard, stream);
	}

	private void ArtifactExportTask(G g, bool withScreenFilter, Artifact artifact, string path)
	{
		using var stream = new FileStream(path, FileMode.Create);
		ArtifactTooltipRenderer.Render(g, withScreenFilter, artifact, stream);
	}

	private void ShipExportTask(G g, bool withScreenFilter, Ship ship, string path)
	{
		using var stream = new FileStream(path, FileMode.Create);
		ShipRenderer.Render(g, withScreenFilter, ship, stream);
	}
}
