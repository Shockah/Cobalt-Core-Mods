using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using Nickel;
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

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;

		var harmony = new Harmony(package.Manifest.UniqueName);
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

	internal void AllCardExportTask(G g)
	{
		var modloaderFolder = AppDomain.CurrentDomain.BaseDirectory;

		static string GetUpgradePathAffix(Upgrade upgrade)
			=> upgrade switch
			{
				Upgrade.A => "A",
				Upgrade.B => "B",
				_ => ""
			};

		List<Upgrade> noUpgrades = [Upgrade.None];

		var groupedCards = DB.cards
			.Select(kvp => (Key: kvp.Key, Type: kvp.Value, Meta: DB.cardMetas.GetValueOrDefault(kvp.Key)))
			.Where(e => e.Meta is not null)
			.Select(e => (Key: e.Key, Type: e.Type, Meta: e.Meta!))
			.Where(e => DB.currentLocale.strings.ContainsKey($"card.{e.Key}.name"))
			.GroupBy(e => e.Meta.deck)
			.Select(g => (Deck: g.Key, HasUnreleased: g.Any(e => e.Meta.unreleased), Entries: g))
			.ToList();

		var exportableData = groupedCards
			.Select(group => new ExportDeckData(
				group.Deck.Key(),
				Loc.T($"char.{group.Deck.Key()}"),
				group.Entries
					.Select(e => new ExportCardData(
						e.Key,
						Loc.T($"card.{e.Key}.name"),
						e.Meta.unreleased,
						e.Meta.rarity,
						noUpgrades.Concat(e.Meta.upgradesTo).ToHashSet(),
						(Activator.CreateInstance(e.Type) as Card)?.GetData(g.state).description
					)).ToList()
			)).ToList();

		var exportableDataPath = Path.Combine(modloaderFolder, "CatDiscordBotDataExport", "cards");
		Directory.CreateDirectory(exportableDataPath);
		File.WriteAllText(Path.Combine(exportableDataPath, "data.json"), JsonConvert.SerializeObject(exportableData, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented
		}));

		foreach (var group in groupedCards)
		{
			var fileSafeDeckKey = group.Deck.Key();
			foreach (var unsafeChar in Path.GetInvalidFileNameChars())
				fileSafeDeckKey = fileSafeDeckKey.Replace(unsafeChar, '_');

			var deckExportPath = Path.Combine(modloaderFolder, "CatDiscordBotDataExport", "cards", fileSafeDeckKey);
			var unreleasedCardsExportPath = Path.Combine(deckExportPath, "unreleased");

			Directory.CreateDirectory(deckExportPath);
			if (group.HasUnreleased)
				Directory.CreateDirectory(unreleasedCardsExportPath);

			foreach (var entry in group.Entries)
			{
				var fileSafeCardKey = entry.Key;
				foreach (var unsafeChar in Path.GetInvalidFileNameChars())
					fileSafeCardKey = fileSafeCardKey.Replace(unsafeChar, '_');

				List<Upgrade> upgrades = [Upgrade.None];
				upgrades.AddRange(entry.Meta.upgradesTo);

				foreach (var upgrade in upgrades)
				{
					var exportPath = Path.Combine(entry.Meta.unreleased ? unreleasedCardsExportPath : deckExportPath, $"{fileSafeCardKey}{GetUpgradePathAffix(upgrade)}.png");
					var card = (Card)Activator.CreateInstance(entry.Type)!;
					card.upgrade = upgrade;
					QueueTask(g => CardExportTask(g, card, exportPath));
				}
			}
		}
	}

	private void CardExportTask(G g, Card card, string path)
	{
		using var stream = new FileStream(path, FileMode.Create);
		CardRenderer.Render(g, card, stream);
	}
}
