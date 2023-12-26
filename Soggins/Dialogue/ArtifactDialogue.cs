using System;

namespace Shockah.Soggins;

internal static class ArtifactDialogue
{
	private static ModEntry Instance => ModEntry.Instance;

	internal static void Inject()
	{
		string soggins = Instance.SogginsDeck.GlobalName;

		foreach (var artifactType in ModEntry.AllArtifacts)
		{
			if (Activator.CreateInstance(artifactType) is not IRegisterableArtifact artifact)
				continue;
			artifact.InjectDialogue();
		}

		DB.story.all[$"ArtifactEnergyPrep_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRun = true,
			lookup = new() { "EnergyPrepTrigger" },
			allPresent = new() { soggins },
			hasArtifacts = new() { "EnergyPrep" },
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "I just got zapped by static.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			}
		};
		DB.story.all[$"ArtifactEnergyRefund_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRun = true,
			oncePerCombatTags = new() { "EnergyRefund" },
			allPresent = new() { soggins },
			hasArtifacts = new() { "EnergyRefund" },
			minCostOfCardJustPlayed = 3,
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "There's free energy laying around.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			}
		};
		DB.story.all[$"ArtifactFractureDetection_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRun = true,
			oncePerCombatTags = new() { "FractureDetectionBarks" },
			allPresent = new() { soggins },
			hasArtifacts = new() { "FractureDetection" },
			maxTurnsThisCombat = 1,
			turnStart = true,
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "Computer, find weak spot.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			}
		};
		DB.story.all[$"ArtifactGeminiCoreBooster_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRunTags = new() { "GeminiCoreBooster" },
			allPresent = new() { soggins },
			hasArtifacts = new() { "GeminiCoreBooster" },
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "Don't worry! This is very simple.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			}
		};
		DB.story.all[$"ArtifactGeminiCore_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRunTags = new() { "GeminiCore" },
			allPresent = new() { soggins },
			hasArtifacts = new() { "GeminiCore" },
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "This ship is simple for a smart person like me.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			}
		};
		DB.story.all[$"ArtifactJumperCables_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRunTags = new() { "ArtifactJumperCablesReady" },
			allPresent = new() { soggins },
			hasArtifacts = new() { "JumperCables" },
			maxTurnsThisCombat = 1,
			maxHullPercent = 0.5,
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "I feel really safe right now.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			}
		};
		DB.story.all[$"ArtifactPowerDiversionMade{soggins}AttackFail"] = new()
		{
			type = NodeType.combat,
			allPresent = new() { soggins, Deck.peri.Key() },
			hasArtifacts = new() { "PowerDiversion" },
			playerShotJustHit = true,
			maxDamageDealtToEnemyThisAction = 0,
			whoDidThat = (Deck)Instance.SogginsDeck.Id!.Value,
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "That's not very nice.",
					loopTag = Instance.MadPortraitAnimation.Tag
				},
				new CustomSay()
				{
					who = Deck.peri.Key(),
					Text = "I'm keeping an eye on you.",
					loopTag = "neutral"
				}
			}
		};
		DB.story.all[$"ArtifactRecalibrator_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = new() { soggins },
			hasArtifacts = new() { "ArtifactRecalibrator" },
			playerShotJustMissed = true,
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "No misses, only happy accidents.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			}
		};
		DB.story.all[$"ArtifactSimplicity_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = new() { soggins },
			hasArtifacts = new() { "Simplicity" },
			oncePerRunTags = new() { "SimplicityShouts" },
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "No thoughts, head empty.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			}
		};
		DB.story.all[$"ArtifactTridimensionalCockpit_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = new() { soggins },
			hasArtifacts = new() { "TridimensionalCockpit" },
			turnStart = true,
			maxTurnsThisCombat = 1,
			oncePerCombatTags = new() { "TridimensionalCockpit" },
			oncePerRun = true,
			lines = new()
			{
				new CustomSay()
				{
					who = soggins,
					Text = "I don't understand where we are right now.",
					loopTag = Instance.MadPortraitAnimation.Tag
				},
				new SaySwitch()
				{
					lines = new()
					{
						new CustomSay()
						{
							who = Deck.hacker.Key(),
							Text = "It's better that way.",
							loopTag = "neutral"
						}
					}
				}
			}
		};
	}
}
