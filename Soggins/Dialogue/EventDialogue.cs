using System.Linq;

namespace Shockah.Soggins;

internal static class EventDialogue
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void Inject()
	{
		string soggins = Instance.SogginsDeck.GlobalName;

		DB.story.GetNode("AbandonedShipyard")?.lines.OfType<SaySwitch>().FirstOrDefault()?.lines.Insert(0, new CustomSay()
		{
			who = soggins,
			Text = "It wasn't me!",
			loopTag = Instance.SmugPortraitAnimations[-2].Tag,
		});
		DB.story.GetNode("AbandonedShipyard_Repaired")?.lines.OfType<SaySwitch>().FirstOrDefault()?.lines.Insert(0, new CustomSay()
		{
			who = soggins,
			Text = "I knew it was a good idea to stop by.",
			loopTag = Instance.SmugPortraitAnimations[2].Tag
		});
		DB.story.all[$"ChoiceCardRewardOfYourColorChoice_{soggins}"] = new()
		{
			type = NodeType.@event,
			oncePerRun = true,
			allPresent = new() { soggins },
			bg = "BGBootSequence",
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "I knew you'd trust my genius ideas.",
					loopTag = Instance.SmugPortraitAnimations[3].Tag
				},
				new CustomSay()
				{
					who = "comp",
					Text = "Energy readings are back to normal."
				}
			}
		};
		DB.story.GetNode("CrystallizedFriendEvent")?.lines.OfType<SaySwitch>().FirstOrDefault()?.lines.Insert(0, new CustomSay()
		{
			who = soggins,
			Text = "My experience will be of use to you all when I'm gone.",
			loopTag = Instance.SmugPortraitAnimations[3].Tag
		});
		DB.story.all[$"CrystallizedFriendEvent_{soggins}"] = new()
		{
			type = NodeType.@event,
			oncePerRun = true,
			allPresent = new() { soggins },
			bg = "BGCrystalizedFriend",
			lines = new()
			{
				new Wait()
				{
					secs = 1.5
				},
				new CustomSay()
				{
					who = soggins,
					Text = "That was the correct choice.",
					loopTag = Instance.SmugPortraitAnimations[3].Tag
				}
			}
		};
		DB.story.GetNode("DraculaTime")?.lines.OfType<SaySwitch>().FirstOrDefault()?.lines.Insert(0, new CustomSay()
		{
			who = soggins,
			Text = "Seems familiar.",
			loopTag = Instance.SquintPortraitAnimation.Tag
		});
		DB.story.GetNode("GrandmaShop")?.lines.OfType<SaySwitch>().FirstOrDefault()?.lines.Insert(0, new CustomSay()
		{
			who = soggins,
			Text = "Your fanciest macarons.",
			loopTag = Instance.SmugPortraitAnimations[3].Tag
		});
		DB.story.GetNode("LoseCharacterCard")?.lines.OfType<SaySwitch>().FirstOrDefault()?.lines.Insert(0, new CustomSay()
		{
			who = soggins,
			Text = "Don't let it eat me!",
			loopTag = Instance.SmugPortraitAnimations[-3].Tag
		});
		DB.story.all[$"LoseCharacterCard_{soggins}"] = new()
		{
			type = NodeType.@event,
			oncePerRun = true,
			allPresent = new() { soggins },
			bg = "BGSupernova",
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "Bye bye.",
					loopTag = Instance.SmugPortraitAnimations[-2].Tag
				},
				new CustomSay()
				{
					who = "comp",
					Text = "Huh, let's get going.",
					loopTag = "squint"
				}
			}
		};
		DB.story.GetNode("Sasha_2_multi_2")?.lines.OfType<SaySwitch>().FirstOrDefault()?.lines.Insert(0, new CustomSay()
		{
			who = soggins,
			Text = "Sports?",
			loopTag = Instance.SquintPortraitAnimation.Tag
		});
		DB.story.all[$"ShopkeeperInfinite_{soggins}_Multi_0"] = new()
		{
			type = NodeType.@event,
			lookup = new() { "shopBefore" },
			allPresent = new() { soggins },
			bg = "BGShop",
			lines = new()
			{
				new CustomSay()
				{
					who = "nerd",
					Text = "Missiles again?",
					loopTag = "neutral"
				},
				new CustomSay()
				{
					who = soggins,
					Text = "Not this time.",
					loopTag = Instance.SmugPortraitAnimations[2].Tag
				},
				new Jump()
				{
					key = "NewShop"
				}
			}
		};
		DB.story.all[$"ShopkeeperInfinite_{soggins}_Multi_1"] = new()
		{
			type = NodeType.@event,
			lookup = new() { "shopBefore" },
			allPresent = new() { soggins },
			bg = "BGShop",
			lines = new()
			{
				new CustomSay()
				{
					who = "nerd",
					Text = "I'm surprised you all made it this far.",
					loopTag = "neutral"
				},
				new CustomSay()
				{
					who = soggins,
					Text = "Huh?",
					loopTag = Instance.MadPortraitAnimation.Tag
				},
				new Jump()
				{
					key = "NewShop"
				}
			}
		};
		DB.story.all[$"ShopkeeperInfinite_{soggins}_Multi_2"] = new()
		{
			type = NodeType.@event,
			lookup = new() { "shopBefore" },
			allPresent = new() { soggins },
			bg = "BGShop",
			lines = new()
			{
				new CustomSay()
				{
					who = "nerd",
					Text = "Hello!",
					loopTag = "neutral"
				},
				new CustomSay()
				{
					who = soggins,
					Text = "Hello!",
					loopTag = Instance.SmugPortraitAnimations[2].Tag
				},
				new Jump()
				{
					key = "NewShop"
				}
			}
		};
		DB.story.GetNode("SogginsEscape_1")?.lines.OfType<SaySwitch>().FirstOrDefault()?.lines.Insert(0, new CustomSay()
		{
			who = soggins,
			Text = "Hey! You're being rude to yourself!",
			loopTag = Instance.MadPortraitAnimation.Tag
		});
		DB.story.all[$"{soggins}_Intro_1"] = new()
		{
			type = NodeType.@event,
			lookup = new() { "zone_first" },
			allPresent = new() { soggins },
			once = true,
			bg = "BGRunStart",
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "Where am I? How did I get here?",
					loopTag = Instance.SmugPortraitAnimations[-3].Tag
				},
				new CustomSay()
				{
					who = "comp",
					Text = "Huh?",
					loopTag = "neutral"
				},
				new CustomSay()
				{
					who = "comp",
					Text = "The timeline must be really messed up or someone is playing a really bad joke.",
					loopTag = "squint"
				},
				new CustomSay()
				{
					who = soggins,
					Text = "Hello?",
					loopTag = Instance.SmugPortraitAnimations[-1].Tag
				},
				new CustomSay()
				{
					who = "comp",
					Text = "Look, just make yourself useful and...",
					loopTag = "neutral"
				},
				new CustomSay()
				{
					who = "comp",
					Text = "Please don't plug weird hard drives into the consoles.",
					loopTag = "squint"
				},
				new CustomSay()
				{
					who = soggins,
					Text = "Okay.",
					loopTag = Instance.SmugPortraitAnimations[-3].Tag
				}
			}
		};
	}
}
