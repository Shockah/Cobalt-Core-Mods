using Nickel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.Johnson;

internal abstract class BaseDialogue(Func<string, Stream> localeStreamFunction)
{
	private readonly INonNullLocalizationProvider<IReadOnlyList<string>> Localizations = new MissingPlaceholderNonBoundLocalizationProvider<IReadOnlyList<string>>(
		new EnglishFallbackLocalizationProvider<IReadOnlyList<string>>(
			new JsonLocalizationProvider(
				tokenExtractor: new SimpleLocalizationTokenExtractor(),
				localeStreamFunction: localeStreamFunction
			)
		)
	);

	protected void InjectStory(Dictionary<IReadOnlyList<string>, StoryNode> newNodes, Dictionary<IReadOnlyList<string>, StoryNode> newHardcodedNodes, Dictionary<IReadOnlyList<string>, Say> saySwitchNodes, NodeType newNodeType)
	{
		var johnsonType = ModEntry.Instance.JohnsonCharacter.CharacterType;

		foreach (var (key, node) in newNodes)
		{
			var realKey = $"{ModEntry.Instance.Package.Manifest.UniqueName}::{string.Join(".", key)}";

			node.type = newNodeType;
			DB.story.all[realKey] = node;

			for (var i = 0; i < node.lines.Count; i++)
				if (node.lines[i] is Say say)
					say.hash = i.ToString();
		}

		foreach (var (key, node) in newHardcodedNodes)
		{
			var realKey = string.Join(".", key.Select(s => s.Replace("{{CharacterType}}", johnsonType)));

			node.type = newNodeType;
			DB.story.all[realKey] = node;

			for (var i = 0; i < node.lines.Count; i++)
				if (node.lines[i] is Say say)
					say.hash = i.ToString();
		}

		foreach (var (key, line) in saySwitchNodes)
		{
			var realKey = string.Join(".", key);
			if (!DB.story.all.TryGetValue(realKey, out var node))
				continue;
			if (node.lines.OfType<SaySwitch>().LastOrDefault() is not { } saySwitch)
				continue;

			if (string.IsNullOrEmpty(line.hash))
				line.hash = $"{johnsonType}::{realKey}";
			saySwitch.lines.Add(line);
		}
	}

	protected void InjectLocalizations(Dictionary<IReadOnlyList<string>, StoryNode> newNodes, Dictionary<IReadOnlyList<string>, StoryNode> newHardcodedNodes, Dictionary<IReadOnlyList<string>, Say> saySwitchNodes, LoadStringsForLocaleEventArgs e)
	{
		var johnsonType = ModEntry.Instance.JohnsonCharacter.CharacterType;

		foreach (var (key, node) in newNodes)
		{
			var realKey = $"{ModEntry.Instance.Package.Manifest.UniqueName}::{string.Join(".", key)}";

			var index = 0;
			foreach (var line in node.lines)
			{
				if (line is Say say)
				{
					e.Localizations[$"{realKey}:{index}"] = Localizations.Localize(e.Locale, [.. key, index.ToString()]);
				}
				else if (line is Wait or Jump)
				{
					index--;
				}
				else
				{
					throw new ArgumentException($"Unhandled story node type {line.GetType().Name} for key {realKey}");
				}
				index++;
			}
		}

		foreach (var (key, node) in newHardcodedNodes)
		{
			var realKey = string.Join(".", key.Select(s => s.Replace("{{CharacterType}}", johnsonType)));

			var index = 0;
			foreach (var line in node.lines)
			{
				if (line is Say say)
				{
					e.Localizations[$"{realKey}:{index}"] = Localizations.Localize(e.Locale, [.. key, index.ToString()]);
				}
				else if (line is Wait or Jump)
				{
					index--;
				}
				else
				{
					throw new ArgumentException($"Unhandled story node type {line.GetType().Name} for key {realKey}");
				}
				index++;
			}
		}

		foreach (var (key, line) in saySwitchNodes)
		{
			var realKey = string.Join(".", key);
			if (string.IsNullOrEmpty(line.hash))
				line.hash = $"{johnsonType}::{realKey}";

			e.Localizations[$"{realKey}:{line.hash}"] = Localizations.Localize(e.Locale, key);
		}
	}
}
