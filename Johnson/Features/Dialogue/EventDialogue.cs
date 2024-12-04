using Nickel;
using System.Collections.Generic;

namespace Shockah.Johnson;

internal sealed class EventDialogue : BaseDialogue
{
	public EventDialogue() : base(locale => ModEntry.Instance.Package.PackageRoot.GetRelativeFile($"i18n/dialogue-event-{locale}.json").OpenRead())
	{
		var johnsonDeck = ModEntry.Instance.JohnsonDeck.Deck;
		var johnsonType = ModEntry.Instance.JohnsonCharacter.CharacterType;
		var newNodes = new Dictionary<IReadOnlyList<string>, StoryNode>();
		var newHardcodedNodes = new Dictionary<IReadOnlyList<string>, StoryNode>();
		var saySwitchNodes = new Dictionary<IReadOnlyList<string>, Say>();

		ModEntry.Instance.Helper.Events.OnModLoadPhaseFinished += (_, phase) =>
		{
			if (phase != ModLoadPhase.AfterDbInit)
				return;
			InjectStory(newNodes, newHardcodedNodes, saySwitchNodes, NodeType.@event);
		};
		ModEntry.Instance.Helper.Events.OnLoadStringsForLocale += (_, e) => InjectLocalizations(newNodes, newHardcodedNodes, saySwitchNodes, e);

		newNodes[["Shop", "0"]] = new()
		{
			lookup = ["shopBefore"],
			bg = typeof(BGShop).Name,
			allPresent = [johnsonType],
			lines = [
				new Say { who = johnsonType, loopTag = "flashing" },
				new Say { who = "nerd", loopTag = "neutral", flipped = true },
				new Jump() { key = "NewShop" }
			],
		};
		newNodes[["Shop", "1"]] = new()
		{
			lookup = ["shopBefore"],
			bg = typeof(BGShop).Name,
			allPresent = [johnsonType],
			lines = [
				new Say { who = johnsonType, loopTag = "neutral" },
				new Say { who = "nerd", loopTag = "neutral", flipped = true },
				new Jump() { key = "NewShop" }
			],
		};

		newHardcodedNodes[["LoseCharacterCard_{{CharacterType}}"]] = new()
		{
			oncePerRun = true,
			bg = typeof(BGSupernova).Name,
			allPresent = [johnsonType],
			lines = [
				new Say { who = johnsonType, loopTag = "neutral" },
			],
		};
		newHardcodedNodes[["CrystallizedFriendEvent_{{CharacterType}}"]] = new()
		{
			oncePerRun = true,
			bg = typeof(BGCrystalizedFriend).Name,
			allPresent = [johnsonType],
			lines = [
				new Wait() { secs = 1.5 },
				new Say { who = johnsonType, loopTag = "fiddling" },
			],
		};
		newHardcodedNodes[["ChoiceCardRewardOfYourColorChoice_{{CharacterType}}"]] = new()
		{
			oncePerRun = true,
			bg = typeof(BGBootSequence).Name,
			allPresent = [johnsonType],
			lines = [
				new Say { who = johnsonType, loopTag = "squint" },
				new Say { who = "comp", loopTag = "neutral" },
			],
		};

		saySwitchNodes[["GrandmaShop"]] = new()
		{
			who = johnsonType,
			loopTag = "neutral"
		};
		saySwitchNodes[["LoseCharacterCard"]] = new()
		{
			who = johnsonType,
			loopTag = "neutral"
		};
		saySwitchNodes[["CrystallizedFriendEvent"]] = new()
		{
			who = johnsonType,
			loopTag = "fiddling"
		};
		saySwitchNodes[["ShopKeepBattleInsult"]] = new()
		{
			who = johnsonType,
			loopTag = "fiddling"
		};
		saySwitchNodes[["DraculaTime"]] = new()
		{
			who = johnsonType,
			loopTag = "squint"
		};
		saySwitchNodes[["Soggins_Infinite"]] = new()
		{
			who = johnsonType,
			loopTag = "flashing"
		};
		saySwitchNodes[["Soggins_Missile_Shout_1"]] = new()
		{
			who = johnsonType,
			loopTag = "neutral"
		};
		saySwitchNodes[["SogginsEscapeIntent_1"]] = new()
		{
			who = johnsonType,
			loopTag = "neutral"
		};
		saySwitchNodes[["SogginsEscape_1"]] = new()
		{
			who = johnsonType,
			loopTag = "fiddling"
		};
	}
}
