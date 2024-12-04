using Nickel;
using System.Collections.Generic;

namespace Shockah.Johnson;

internal sealed class CardDialogue : BaseDialogue
{
	public CardDialogue() : base(locale => ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"i18n/dialogue-card-{locale}.json").OpenRead())
	{
		var johnsonDeck = ModEntry.Instance.JohnsonDeck.Deck;
		var johnsonType = ModEntry.Instance.JohnsonCharacter.CharacterType;
		var newNodes = new Dictionary<IReadOnlyList<string>, StoryNode>();

		ModEntry.Instance.Helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			InjectStory(newNodes, [], [], NodeType.combat);
		};
		ModEntry.Instance.Helper.Events.OnLoadStringsForLocale += (_, e) => InjectLocalizations(newNodes, [], [], e);

		newNodes[["Played", "Quarter1"]] = new()
		{
			lookup = [$"Played::{new Quarter1Card().Key()}"],
			priority = true,
			oncePerRun = true,
			allPresent = [johnsonType],
			lines = [
				new Say { who = johnsonType, loopTag = "fiddling" },
			],
		};

		for (var i = 0; i < 3; i++)
			newNodes[["Played", "Deadline", i.ToString()]] = new()
			{
				lookup = [$"Played::{new DeadlineCard().Key()}"],
				priority = true,
				oncePerRun = true,
				oncePerCombatTags = [$"Played::{new DeadlineCard().Key()}"],
				allPresent = [johnsonType],
				lines = [
					new Say { who = johnsonType, loopTag = "fiddling" },
				],
			};

		for (var i = 0; i < 3; i++)
			newNodes[["Played", "LayoutOrStrategize", i.ToString()]] = new()
			{
				lookup = [$"Played::{ModEntry.Instance.Package.Manifest.UniqueName}::LayoutOrStrategize"],
				priority = true,
				oncePerRun = true,
				oncePerCombatTags = [$"Played::{ModEntry.Instance.Package.Manifest.UniqueName}::LayoutOrStrategize"],
				allPresent = [johnsonType],
				lines = [
					new Say { who = johnsonType, loopTag = "neutral" },
				],
			};

		for (var i = 0; i < 2; i++)
			newNodes[["Played", "Downsize", i.ToString()]] = new()
			{
				lookup = [$"Played::{new DownsizeCard().Key()}"],
				priority = true,
				oncePerRun = true,
				oncePerCombatTags = [$"Played::{new DownsizeCard().Key()}"],
				allPresent = [johnsonType],
				lines = [
					new Say { who = johnsonType, loopTag = "fiddling" },
				],
			};
	}
}
