namespace Shockah.Bloch;

internal sealed class BasicDialogue
{
	public BasicDialogue()
	{
		var deck = ModEntry.Instance.BlochDeck.Deck;
		var key = ModEntry.Instance.BlochDeck.UniqueName;

		DB.story.all[$"{key}_Intro_1"] = new()
		{
			type = NodeType.@event,
			lookup = ["zone_first"],
			allPresent = [key],
			once = true,
			bg = "BGRunStart",
			lines = [
				new LocalizedSay()
				{
					who = key,
					loopTag = "glorp",
					Key = ["Intro_1", "1-Bloch"],
				},
				new LocalizedSay()
				{
					who = "comp",
					loopTag = "neutral",
					Key = ["Intro_1", "2-CAT"],
				},
				new LocalizedSay()
				{
					who = key,
					loopTag = "glerp",
					Key = ["Intro_1", "3-Bloch"],
				},
				new LocalizedSay()
				{
					who = "comp",
					loopTag = "mad",
					Key = ["Intro_1", "4-CAT"],
				},
				new LocalizedSay()
				{
					who = key,
					loopTag = "talking",
					Key = ["Intro_1", "5-Bloch"],
				},
				new LocalizedSay()
				{
					who = "comp",
					loopTag = "neutral",
					Key = ["Intro_1", "6-CAT"],
				},
				new LocalizedSay()
				{
					who = "comp",
					loopTag = "squint",
					Key = ["Intro_1", "7-CAT"],
				},
				new LocalizedSay()
				{
					who = key,
					loopTag = "gloop",
					Key = ["Intro_1", "8-Bloch"],
				}
			]
		};
	}
}
