namespace Shockah.Soggins;

internal static class SmugDialogue
{
	private static ModEntry Instance => ModEntry.Instance;

	private static readonly string[] BotchLines = new string[]
	{
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
		"Pwease?"
	};
	private static readonly string[] DoubleLines = new string[]
	{
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
		"That was planned."
	};

	internal static void Inject()
	{
		string soggins = Instance.SogginsDeck.GlobalName;

		for (int i = 0; i < BotchLines.Length; i++)
			DB.story.all[$"{soggins}_Botch_{i}"] = new()
			{
				type = NodeType.combat,
				lookup = new() { $"{soggins}_Botch" },
				allPresent = new() { soggins },
				lines = new()
				{
					new CustomSay()
					{
						who = soggins,
						Text = BotchLines[i],
						DynamicLoopTag = Dialogue.CurrentSmugLoopTag
					}
				}
			};

		for (int i = 0; i < DoubleLines.Length; i++)
			DB.story.all[$"{soggins}_Double_{i}"] = new()
			{
				type = NodeType.combat,
				lookup = new() { $"{soggins}_Double" },
				allPresent = new() { soggins },
				lines = new()
				{
					new CustomSay()
					{
						who = soggins,
						Text = DoubleLines[i],
						DynamicLoopTag = Dialogue.CurrentSmugLoopTag
					}
				}
			};
	}
}
