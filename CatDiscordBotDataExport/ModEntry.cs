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

namespace Shockah.CatDiscordBotDataExport;

internal sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;

	private readonly Queue<Action<G>> QueuedTasks = new();
	internal readonly CardRenderer CardRenderer = new();
	internal readonly CardTooltipRenderer CardTooltipRenderer = new();

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;

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
			Logger!.LogInformation("Finished all tasks.");
		else if (QueuedTasks.Count % 25 == 0)
			Logger!.LogInformation("Tasks left in the queue: {TaskCount}", QueuedTasks.Count);
	}

	internal void QueueSelectedDecksExportTask(G g, bool withScreenFilter)
	{
		if (g.metaRoute?.subRoute is Codex { subRoute: CardBrowse cardCodex })
		{
			foreach (var deck in cardCodex.GetCardList(g).Select(c => c.GetMeta().deck).ToHashSet())
				QueueDeckExportTask(g, withScreenFilter, deck);
		}
		else if (g.state.IsOutsideRun())
		{
			foreach (var deck in g.state.runConfig.selectedChars)
				QueueDeckExportTask(g, withScreenFilter, deck);
		}
		else
		{
			foreach (var character in g.state.characters)
				if (character.deckType is { } deck)
					QueueDeckExportTask(g, withScreenFilter, deck);
		}
	}

	internal void QueueDeckExportTask(G g, bool withScreenFilter, Deck deck)
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

		List<Upgrade> noUpgrades = [Upgrade.None];

		var cards = DB.cards
			.Select(kvp => (Key: kvp.Key, Type: kvp.Value, Meta: DB.cardMetas.GetValueOrDefault(kvp.Key)))
			.Where(e => e.Meta is not null)
			.Select(e => (Key: e.Key, Type: e.Type, Meta: e.Meta!))
			.Where(e => e.Meta.deck == deck)
			.Where(e => DB.currentLocale.strings.ContainsKey($"card.{e.Key}.name"))
			.ToList();

		var exportableData = new ExportDeckData(
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
									if (t.Configuration.Name is not { } nameProvider || nameProvider("en") is not { } name || string.IsNullOrEmpty(name))
										return null;
									return new ExportCardTraitData(Key: t.UniqueName, Name: name);
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
		if (exportableData.Cards.Any(m => m.Released && !m.Offered))
			Directory.CreateDirectory(Path.Combine(deckExportPath, "unoffered"));

		foreach (var exportableCard in exportableData.Cards)
		{
			var card = (Card)Activator.CreateInstance(DB.cards.First(e => e.Key == exportableCard.Key).Value)!;
			foreach (var exportableUpgrade in exportableCard.Upgrades)
			{
				var cardAtUpgrade = Mutil.DeepCopy(card);
				cardAtUpgrade.upgrade = exportableUpgrade.Key;

				QueueTask(g => CardExportTask(g, withScreenFilter, cardAtUpgrade, Path.Combine(deckExportPath, exportableUpgrade.Value.BaseImagePath)));
				QueueTask(g => TooltipExportTask(g, withScreenFilter, cardAtUpgrade, withTheCard: true, Path.Combine(deckExportPath, exportableUpgrade.Value.TooltipImagePath)));
			}
		}
	}

	private void CardExportTask(G g, bool withScreenFilter, Card card, string path)
	{
		using var stream = new FileStream(path, FileMode.Create);
		CardRenderer.Render(g, withScreenFilter, card, stream);
	}

	private void TooltipExportTask(G g, bool withScreenFilter, Card card, bool withTheCard, string path)
	{
		using var stream = new FileStream(path, FileMode.Create);
		CardTooltipRenderer.Render(g, withScreenFilter, card, withTheCard, stream);
	}
}
