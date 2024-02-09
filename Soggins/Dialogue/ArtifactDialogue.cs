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
			allPresent = [soggins],
			hasArtifacts = ["EnergyPrep"],
			lines = [
				new CustomSay()
				{
					who = soggins,
					Text = "I just got zapped by static.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ArtifactEnergyRefund_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRun = true,
			oncePerCombatTags = ["EnergyRefund"],
			allPresent = [soggins],
			hasArtifacts = ["EnergyRefund"],
			minCostOfCardJustPlayed = 3,
			lines = [
				new CustomSay()
				{
					who = soggins,
					Text = "There's free energy laying around.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ArtifactFractureDetection_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRun = true,
			oncePerCombatTags = ["FractureDetectionBarks"],
			allPresent = [soggins],
			hasArtifacts = ["FractureDetection"],
			maxTurnsThisCombat = 1,
			turnStart = true,
			lines = [
				new CustomSay()
				{
					who = soggins,
					Text = "Computer, find weak spot.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ArtifactGeminiCoreBooster_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRunTags = ["GeminiCoreBooster"],
			allPresent = [soggins],
			hasArtifacts = ["GeminiCoreBooster"],
			lines = [
				new CustomSay()
				{
					who = soggins,
					Text = "Don't worry! This is very simple.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ArtifactGeminiCore_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRunTags = ["GeminiCore"],
			allPresent = [soggins],
			hasArtifacts = ["GeminiCore"],
			lines = [
				new CustomSay()
				{
					who = soggins,
					Text = "This ship is simple for a smart person like me.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ArtifactJumperCables_{soggins}"] = new()
		{
			type = NodeType.combat,
			oncePerRunTags = ["ArtifactJumperCablesReady"],
			allPresent = [soggins],
			hasArtifacts = ["JumperCables"],
			maxTurnsThisCombat = 1,
			maxHullPercent = 0.5,
			lines = [
				new CustomSay()
				{
					who = soggins,
					Text = "I feel really safe right now.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ArtifactPowerDiversionMade{soggins}AttackFail"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins, Deck.peri.Key()],
			hasArtifacts = ["PowerDiversion"],
			playerShotJustHit = true,
			maxDamageDealtToEnemyThisAction = 0,
			whoDidThat = (Deck)Instance.SogginsDeck.Id!.Value,
			lines = [
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
			]
		};
		DB.story.all[$"ArtifactRecalibrator_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			hasArtifacts = ["ArtifactRecalibrator"],
			playerShotJustMissed = true,
			lines = [
				new CustomSay()
				{
					who = soggins,
					Text = "No misses, only happy accidents.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ArtifactSimplicity_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			hasArtifacts = ["Simplicity"],
			oncePerRunTags = ["SimplicityShouts"],
			lines = [
				new CustomSay()
				{
					who = soggins,
					Text = "No thoughts, head empty.",
					DynamicLoopTag = Dialogue.CurrentSmugLoopTag
				}
			]
		};
		DB.story.all[$"ArtifactTridimensionalCockpit_{soggins}"] = new()
		{
			type = NodeType.combat,
			allPresent = [soggins],
			hasArtifacts = ["TridimensionalCockpit"],
			turnStart = true,
			maxTurnsThisCombat = 1,
			oncePerCombatTags = ["TridimensionalCockpit"],
			oncePerRun = true,
			lines = [
				new CustomSay()
				{
					who = soggins,
					Text = "I don't understand where we are right now.",
					loopTag = Instance.MadPortraitAnimation.Tag
				},
				new SaySwitch()
				{
					lines = [
						new CustomSay()
						{
							who = Deck.hacker.Key(),
							Text = "It's better that way.",
							loopTag = "neutral"
						}
					]
				}
			]
		};
	}
}
