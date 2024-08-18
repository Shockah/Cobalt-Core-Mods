using System.Linq;

namespace Shockah.Soggins;

internal static class CombatDialogue
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void Inject()
	{
		var soggins = Instance.SogginsDeck.GlobalName;

		DB.story.all[$"BlockedALotOfAttacksWithArmor_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			oncePerRun = true,
			oncePerCombatTags = ["YowzaThatWasALOTofArmorBlock"],
			enemyShotJustHit = true,
			minDamageBlockedByPlayerArmorThisTurn = 3,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "All of that was planned.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"DizzyWentMissing_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			oncePerCombatTags = ["dizzyWentMissing"],
			lastTurnPlayerStatuses = [Status.missingDizzy],
			priority = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Better him than me.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"RiggsWentMissing_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			oncePerCombatTags = ["riggsWentMissing"],
			lastTurnPlayerStatuses = [Status.missingRiggs],
			priority = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Bring me a drink when you come back.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"PeriWentMissing_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			oncePerCombatTags = ["periWentMissing"],
			lastTurnPlayerStatuses = [Status.missingPeri],
			priority = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "She was scary anyway.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"IsaacWentMissing_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			oncePerCombatTags = ["isaacWentMissing"],
			lastTurnPlayerStatuses = [Status.missingIsaac],
			priority = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "He's coming back, it'll be fine.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"DrakeWentMissing_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			oncePerCombatTags = ["drakeWentMissing"],
			lastTurnPlayerStatuses = [Status.missingDrake],
			priority = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "She was scary anyway.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"MaxWentMissing_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			oncePerCombatTags = ["maxWentMissing"],
			lastTurnPlayerStatuses = [Status.missingMax],
			priority = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Mortimer?",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"BooksWentMissing_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			oncePerCombatTags = ["booksWentMissing"],
			lastTurnPlayerStatuses = [Status.missingBooks],
			priority = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Nice magic trick, kid.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"CatWentMissing_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			oncePerCombatTags = ["CatWentMissing"],
			lastTurnPlayerStatuses = [Status.missingCat],
			priority = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "How do you reboot the computer?",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.GetNode("CrabFacts1_Multi_0")?.lines.OfType<SaySwitch>().LastOrDefault()?.lines.Insert(0, new CustomSay
		{
			who = soggins,
			Text = "Do you have frog facts?",
			DynamicLoopTag = Dialogue.CurrentSmugLoopTag
		});
		DB.story.GetNode("CrabFacts2_Multi_0")?.lines.OfType<SaySwitch>().LastOrDefault()?.lines.Insert(0, new CustomSay
		{
			who = soggins,
			Text = "We're so alike!",
			DynamicLoopTag = Dialogue.CurrentSmugLoopTag
		});
		DB.story.GetNode("CrabFactsAreOverNow_Multi_0")?.lines.OfType<SaySwitch>().LastOrDefault()?.lines.Insert(0, new CustomSay
		{
			who = soggins,
			Text = "I knew all of those facts already.",
			DynamicLoopTag = Dialogue.CurrentSmugLoopTag
		});
		DB.story.all[$"{soggins}JustHit_0"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			whoDidThat = (Deck)Instance.SogginsDeck.Id!.Value,
			playerShotJustHit = true,
			minDamageDealtToEnemyThisAction = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "As expected.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = Deck.peri.Key(),
							Text = "...",
							loopTag = "squint"
						},
						new CustomSay
						{
							who = Deck.shard.Key(),
							Text = "!",
							loopTag = "stoked"
						}
					]
				}
			]
		};
		DB.story.all[$"{soggins}JustHit_1"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			whoDidThat = (Deck)Instance.SogginsDeck.Id!.Value,
			playerShotJustHit = true,
			minDamageDealtToEnemyThisAction = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Good!",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = Deck.eunice.Key(),
							Text = "You somehow did it.",
							loopTag = "sly"
						},
						new CustomSay
						{
							who = Deck.riggs.Key(),
							Text = "Wow!",
							loopTag = "neutral"
						}
					]
				}
			]
		};
		DB.story.all[$"{soggins}JustHit_2"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			whoDidThat = (Deck)Instance.SogginsDeck.Id!.Value,
			playerShotJustHit = true,
			minDamageDealtToEnemyThisAction = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Blam!",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = "comp",
							Text = "You can do things right sometimes.",
							loopTag = "squint"
						},
						new CustomSay
						{
							who = Deck.goat.Key(),
							Text = "I have to log the successes.",
							loopTag = "writing"
						}
					]
				}
			]
		};
		DB.story.all[$"{soggins}JustHit_3"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			whoDidThat = (Deck)Instance.SogginsDeck.Id!.Value,
			playerShotJustHit = true,
			minDamageDealtToEnemyThisAction = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "I'm unmatched.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = Deck.dizzy.Key(),
							Text = "You did it!",
							loopTag = "neutral"
						},
						new CustomSay
						{
							who = Deck.hacker.Key(),
							Text = "Huh, good job.",
							loopTag = "neutral"
						}
					]
				}
			]
		};
		DB.story.all[$"JustHitGeneric_{soggins}_0"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisAction = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "I helped.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"JustHitGeneric_{soggins}_1"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisAction = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "All me.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"JustHitGeneric_{soggins}_2"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisAction = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "I'm so good at this.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"JustHitGeneric_{soggins}_3"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisAction = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "I did it!",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"JustHitGeneric_{soggins}_4"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisAction = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "All according to plan.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"Duo_AboutToDieAndLoop_{soggins}0"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			maxHull = 2,
			oncePerCombatTags = ["aboutToDie"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Guess I'll see you all next loop.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = Deck.dizzy.Key(),
							Text = "Guess again.",
							loopTag = "neutral"
						},
						new CustomSay
						{
							who = Deck.riggs.Key(),
							Text = "Don't count on it.",
							loopTag = "neutral"
						},
						new CustomSay
						{
							who = Deck.peri.Key(),
							Text = "I'd rather not.",
							loopTag = "squint"
						},
						new CustomSay
						{
							who = Deck.eunice.Key(),
							Text = "Can we just loop already?",
							loopTag = "mad"
						},
						new CustomSay
						{
							who = Deck.shard.Key(),
							Text = "We'll try again extra hard next time!",
							loopTag = "neutral"
						},
						new CustomSay
						{
							who = "comp",
							Text = "I still want to know how you got in here.",
							loopTag = "squint"
						}
					]
				}
			]
		};
		DB.story.all[$"Duo_AboutToDieAndLoop_{soggins}1"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, "comp"],
			maxHull = 2,
			oncePerCombatTags = ["aboutToDie"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = "comp",
					Text = "Someone expedited our demise.",
					loopTag = "squint"
				},
				new CustomSay
				{
					who = soggins,
					Text = "I wonder who that is.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"Duo_AboutToDieAndLoop_{soggins}2"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, Deck.dizzy.Key()],
			maxHull = 2,
			oncePerCombatTags = ["aboutToDie"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = Deck.dizzy.Key(),
					Text = "This isn't looking pretty.",
					loopTag = "neutral"
				},
				new CustomSay
				{
					who = soggins,
					Text = "What do you mean I don't look pretty.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"Duo_AboutToDieAndLoop_{soggins}3"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, Deck.riggs.Key()],
			maxHull = 2,
			oncePerCombatTags = ["aboutToDie"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = Deck.riggs.Key(),
					Text = "We have to do something!",
					loopTag = "neutral"
				},
				new CustomSay
				{
					who = soggins,
					Text = "I'll press some buttons!",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"Duo_AboutToDieAndLoop_{soggins}4"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, Deck.peri.Key()],
			maxHull = 2,
			oncePerCombatTags = ["aboutToDie"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = Deck.peri.Key(),
					Text = "This is getting pretty dire.",
					loopTag = "mad"
				},
				new CustomSay
				{
					who = soggins,
					Text = "Don't worry, I'm here with you all.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"Duo_AboutToDieAndLoop_{soggins}5"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, Deck.goat.Key()],
			maxHull = 2,
			oncePerCombatTags = ["aboutToDie"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = Deck.goat.Key(),
					Text = "Is there a way out of this mess?",
					loopTag = "panic"
				},
				new CustomSay
				{
					who = soggins,
					Text = "Trust me, it's fine.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"Duo_AboutToDieAndLoop_{soggins}6"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, Deck.eunice.Key()],
			maxHull = 2,
			oncePerCombatTags = ["aboutToDie"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = Deck.eunice.Key(),
					Text = "I'm blaming you.",
					loopTag = "mad"
				},
				new CustomSay
				{
					who = soggins,
					Text = "I haven't done anything wrong!",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"Duo_AboutToDieAndLoop_{soggins}7"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, Deck.hacker.Key()],
			maxHull = 2,
			oncePerCombatTags = ["aboutToDie"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = Deck.hacker.Key(),
					Text = "I'm going to die and my computer is full of viruses.",
					loopTag = "mad"
				},
				new CustomSay
				{
					who = soggins,
					Text = "You still have games on your phone.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"Duo_AboutToDieAndLoop_{soggins}8"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, Deck.shard.Key()],
			maxHull = 2,
			oncePerCombatTags = ["aboutToDie"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = Deck.shard.Key(),
					Text = "Use your magic powers to save us!",
					loopTag = "neutral"
				},
				new CustomSay
				{
					who = soggins,
					Text = "Yes, magic powers.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"EmptyHandWithEnergy_{soggins}_0"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			handEmpty = true,
			minEnergy = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "That extra energy could heat up the hot tub.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"EmptyHandWithEnergy_{soggins}_1"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			handEmpty = true,
			minEnergy = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Yep, that was planned.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"EnemyArmorHitLots_{soggins}_0"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageBlockedByEnemyArmorThisTurn = 3,
			oncePerCombat = true,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "That sound is funny.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"EnemyArmorHitLots_{soggins}_1"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageBlockedByEnemyArmorThisTurn = 3,
			oncePerCombat = true,
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "We're doing big damage now.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ExpensiveCardPlayed_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			minCostOfCardJustPlayed = 4,
			oncePerCombatTags = ["ExpensiveCardPlayed"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "My game just shut itself off.",
					loopTag = Instance.MadPortraitAnimation.Tag
				}
			]
		};
		DB.story.all[$"HandOnlyHasTrashCards_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			handFullOfTrash = true,
			oncePerCombatTags = ["handOnlyHasTrashCards"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "This is just fine.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"HandOnlyHasUnplayableCards_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			handFullOfUnplayableCards = true,
			oncePerCombatTags = ["handFullOfUnplayableCards"],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Do the buttons do anything?",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeDontOverlapWithEnemyAtAllButWeDoHaveASeekerToDealWith_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			shipsDontOverlapAtAll = true,
			oncePerCombatTags = ["NoOverlapBetweenShipsSeeker"],
			anyDronesHostile = ["missile_seeker"],
			nonePresent = ["crab"],
			priority = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Seeker turn off how?",
					loopTag = Instance.SmugPortraitAnimations[-2].Tag
				},
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = Deck.riggs.Key(),
							Text = "I don't think asking it will solve anything.",
							loopTag = "neutral"
						}
					]
				}
			]
		};
		DB.story.all[$"ManyTurns_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			minTurnsThisCombat = 9,
			oncePerCombatTags = ["manyTurns"],
			oncePerRun = true,
			turnStart = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Mind if I just play a game on the side?",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"OverheatCatFix_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, "comp"],
			wasGoingToOverheatButStopped = true,
			whoDidThat = Deck.colorless,
			oncePerCombatTags = ["OverheatCatFix"],
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "My hot tub is getting cold.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"OverheatDrakeFix_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, Deck.eunice.Key()],
			wasGoingToOverheatButStopped = true,
			whoDidThat = Deck.eunice,
			oncePerCombatTags = ["OverheatDrakeFix"],
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "My hot tub is getting cold.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"OverheatGeneric_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			goingToOverheat = true,
			oncePerCombatTags = ["OverheatGeneric"],
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Is it getting hot here?",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ThatsALotOfDamageToThem_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisTurn = 10,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "You can all thank me later.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ThatsALotOfDamageToUs_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			enemyShotJustHit = true,
			minDamageDealtToPlayerThisTurn = 3,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Oh no!",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"TookZeroDamageAtLowHealth_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			enemyShotJustHit = true,
			maxDamageDealtToPlayerThisTurn = 0,
			maxHull = 2,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Yup, perfectly fine.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeAreCorroded_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			lastTurnPlayerStatuses = [Status.corrode],
			oncePerRun = true,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "The walls are looking green, I like green.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeMissedOopsie_{soggins}_0"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustMissed = true,
			oncePerCombat = true,
			doesNotHaveArtifacts = ["Recalibrator", "GrazerBeam"],
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Oops!",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = Deck.peri.Key(),
							Text = "Stop wasting our ammo.",
							loopTag = "squint"
						},
						new CustomSay
						{
							who = Deck.eunice.Key(),
							Text = "It would be cute if it wasn't losing our time.",
							loopTag = "squint"
						}
					]
				}
			]
		};
		DB.story.all[$"WeMissedOopsie_{soggins}_1"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustMissed = true,
			oncePerCombat = true,
			doesNotHaveArtifacts = ["Recalibrator", "GrazerBeam"],
			lines = [
				new SaySwitch
				{
					lines = [
						new CustomSay
						{
							who = Deck.peri.Key(),
							Text = "Recalculating.",
							loopTag = "neutral"
						},
						new CustomSay
						{
							who = Deck.eunice.Key(),
							Text = "Drat.",
							loopTag = "mad"
						}
					]
				},
				new CustomSay
				{
					who = soggins,
					Text = "Let me fire instead.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeGotHurtButNotTooBad_{soggins}_0"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			enemyShotJustHit = true,
			minDamageDealtToPlayerThisTurn = 1,
			maxDamageDealtToPlayerThisTurn = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "We'll be fine.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeGotHurtButNotTooBad_{soggins}_1"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			enemyShotJustHit = true,
			minDamageDealtToPlayerThisTurn = 1,
			maxDamageDealtToPlayerThisTurn = 1,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "I know a place we can go for repairs.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeDidOverThreeDamage_{soggins}_0"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisTurn = 4,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "That was a bazillion damage.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeDidOverThreeDamage_{soggins}_1"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisTurn = 4,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Winning!",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeDidOverThreeDamage_{soggins}_2"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisTurn = 4,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Dealing damage.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeDidOverFiveDamage_{soggins}_0"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisTurn = 6,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "I'm so good.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeDidOverFiveDamage_{soggins}_1"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisTurn = 6,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "That was a thousand bazillion damage.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeDidOverFiveDamage_{soggins}_2"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisTurn = 6,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "One more win for me.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"WeDidOverFiveDamage_{soggins}_3"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, "comp"],
			playerShotJustHit = true,
			minDamageDealtToEnemyThisTurn = 6,
			lines = [
				new CustomSay
				{
					who = soggins,
					Text = "Just according to keikaku.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				},
				new CustomSay
				{
					who = "comp",
					Text = "Keikaku means... How do you even know that word?",
					loopTag = "squint"
				}
			]
		};
	}
}
