using System.Collections.Generic;

namespace Shockah.Soggins;

internal static class SmugDialogue
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly string[] BotchLines = [
		"I'm helping!",
		"That was good, right?",
		"Did that do anything?",
		"Oh no!",
		"Do something!",
		"I'm pressing buttons!",
		"This is bugged.",
		"I didn't break it.",
		"Didn't this work last time?",
		"The manual didn't explain this.",
		"Hello??",
		"The buttons, they do nothing!",
		"This looked so easy.",
		"Sowwee.",
		"Pwease?",
	];

	private static readonly string[] DoubleLines = [
		"I'm helping!",
		"That was good.",
		"I'm so good at this.",
		"Wow! It works!",
		"It's doing something.",
		"I'm pressing buttons.",
		"This is working!",
		"I didn't break it this time.",
		"I'm a genius.",
		"I totally knew it.",
		"Thank me later.",
		"It just works.",
		"That was easy.",
		"What happened? I was distracted.",
		"That was planned.",
	];

	private static readonly Dictionary<Deck, (string LoopTag, string Text)[]> BotchResponseLines = new()
	{
		{
			Deck.dizzy, new (string LoopTag, string Text)[]
			{
				("squint", "This is going to be an interesting experience."),
				("squint", "You can stop that now."),
				("neutral", "Are you interested in becoming a live specimen?"),
			}
		},
		{
			Deck.riggs, new (string LoopTag, string Text)[]
			{
				("squint", "I wanted to use that."),
				("neutral", "Can we put him into the brig?"),
				("neutral", "Is this a prank?"),
			}
		},
		{
			Deck.peri, new (string LoopTag, string Text)[]
			{
				("squint", "..."),
				("mad", "Take your hands off my controls."),
				("squint", "Could you sit down?"),
			}
		},
		{
			Deck.goat, new (string LoopTag, string Text)[]
			{
				("panic", "This is not fine!"),
				("squint", "Oh man..."),
				("squint", "Oh no."),
			}
		},
		{
			Deck.eunice, new (string LoopTag, string Text)[]
			{
				("mad", "I'm going to smack you."),
				("mad", "This is your fault."),
				("squint", "Ugh."),
			}
		},
		{
			Deck.hacker, new (string LoopTag, string Text)[]
			{
				("squint", "My controls are all slimy."),
				("squint", "I need to put a lock on his door."),
				("mad", "Please stop."),
			}
		},
		{
			Deck.shard, new (string LoopTag, string Text)[]
			{
				("squint", "The prince is being unhelpful!"),
				("neutral", "I'll accept your apology."),
				("neutral", "As long as you say sorry!"),
			}
		},
		{
			Deck.colorless, new (string LoopTag, string Text)[]
			{
				("squint", "Error 404."),
				("squint", "Null exception."),
				("lean", "Could you stop?"),
			}
		},
	};

	private static readonly Dictionary<Deck, (string LoopTag, string Text)[]> DoubleResponseLines = new()
	{
		{
			Deck.dizzy, new (string LoopTag, string Text)[]
			{
				("explains", "Science!"),
				("shrug", "Science?"),
				("neutral", "Unexpected!"),
			}
		},
		{
			Deck.riggs, new (string LoopTag, string Text)[]
			{
				("neutral", "Wow!"),
				("neutral", "That was something!"),
				("neutral", "That was good?"),
			}
		},
		{
			Deck.peri, new (string LoopTag, string Text)[]
			{
				("neutral", "..."),
				("neutral", "I don't know how to feel about this."),
				("neutral", "I wasn't prepared for that."),
			}
		},
		{
			Deck.goat, new (string LoopTag, string Text)[]
			{
				("squint", "Uh huh..."),
				("squint", "Ummm."),
			}
		},
		{
			Deck.eunice, new (string LoopTag, string Text)[]
			{
				("neutral", "Hell yeah!"),
				("sly", "Heheh."),
				("neutral", "Don't stop whatever you're doing."),
			}
		},
		{
			Deck.hacker, new (string LoopTag, string Text)[]
			{
				("neutral", "Hmmm."),
				("neutral", "That's okay?"),
				("squint", "Oookay."),
			}
		},
		{
			Deck.shard, new (string LoopTag, string Text)[]
			{
				("stoked", "That was doubly amazing!"),
				("relaxed", "Thank you, Mr. Prince."),
				("stoked", "Yay!"),
			}
		},
		{
			Deck.colorless, new (string LoopTag, string Text)[]
			{
				("squint", "What are you even doing?"),
				("squint", "Stop bashing the console!"),
				("squint", "Huuuh."),
			}
		},
	};

	internal static void Inject()
	{
		string soggins = Instance.SogginsDeck.GlobalName;

		for (int i = 0; i < BotchLines.Length; i++)
			DB.story.all[$"{soggins}_Botch_{i}"] = new()
			{
				type = NodeType.combat,
				lookup = [$"{soggins}_Botch"],
				allPresent = [soggins],
				oncePerCombat = true,
				lines = [
					new CustomSay()
					{
						who = soggins,
						Text = BotchLines[i],
						DynamicLoopTag = Dialogue.CurrentSmugLoopTag
					}
				]
			};

		for (int i = 0; i < DoubleLines.Length; i++)
			DB.story.all[$"{soggins}_Double_{i}"] = new()
			{
				type = NodeType.combat,
				lookup = [$"{soggins}_Double"],
				allPresent = [soggins],
				oncePerCombat = true,
				lines = [
					new CustomSay()
					{
						who = soggins,
						Text = DoubleLines[i],
						DynamicLoopTag = Dialogue.CurrentSmugLoopTag
					}
				]
			};

		foreach (var (deck, entries) in BotchResponseLines)
		{
			var deckKey = deck == Deck.colorless ? "comp" : deck.Key();
			for (int i = 0; i < entries.Length; i++)
				DB.story.all[$"{soggins}_BotchResponse_{deckKey}_{i}"] = new()
				{
					type = NodeType.combat,
					lookup = [$"{soggins}_BotchResponse_{deckKey}"],
					allPresent = [deckKey],
					oncePerCombat = true,
					lines = [
						new CustomSay()
						{
							who = deckKey,
							Text = entries[i].Text,
							loopTag = entries[i].LoopTag
						}
					]
				};
		}

		foreach (var (deck, entries) in DoubleResponseLines)
		{
			var deckKey = deck == Deck.colorless ? "comp" : deck.Key();
			for (int i = 0; i < entries.Length; i++)
				DB.story.all[$"{soggins}_DoubleResponse_{deckKey}_{i}"] = new()
				{
					type = NodeType.combat,
					lookup = [$"{soggins}_DoubleResponse"],
					allPresent = [deckKey],
					oncePerCombat = true,
					lines = [
						new CustomSay()
						{
							who = deckKey,
							Text = entries[i].Text,
							loopTag = entries[i].LoopTag
						}
					]
				};
		}

		DB.story.all[$"{soggins}_DoubleLaunchResponse_0"] = new()
		{
			type = NodeType.combat,
			lookup = [$"{soggins}_DoubleLaunchResponse"],
			allPresent = [Deck.goat.Key()],
			oncePerCombat = true,
			priority = true,
			lines = [
				new CustomSay()
				{
					who = Deck.goat.Key(),
					Text = "That's not how things normally work.",
					loopTag = "panic"
				}
			]
		};
		DB.story.all[$"{soggins}_DoubleLaunchResponse_1"] = new()
		{
			type = NodeType.combat,
			lookup = [$"{soggins}_DoubleLaunchResponse"],
			allPresent = [Deck.goat.Key()],
			oncePerCombat = true,
			priority = true,
			lines = [
				new CustomSay()
				{
					who = Deck.goat.Key(),
					Text = "It moved out of the way, huh?",
					loopTag = "squint"
				}
			]
		};
	}
}
