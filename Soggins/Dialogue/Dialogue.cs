using System;

namespace Shockah.Soggins;

internal static class Dialogue
{
	internal const string CurrentSmugLoopTag = "CurrentSmugLoopTag";

	private static ModEntry Instance => ModEntry.Instance;

	internal static void Inject()
	{
		CustomSay.RegisteredDynamicLoopTags[CurrentSmugLoopTag] = CurrentSmugLoopTagFunction;

		EventDialogue.Inject();
		SmugDialogue.Inject();
		ArtifactDialogue.Inject();
		CombatDialogue.Inject();

		foreach (var cardType in ModEntry.AllCards)
		{
			if (Activator.CreateInstance(cardType) is not IRegisterableCard card)
				continue;
			card.InjectDialogue();
		}
	}

	private static string CurrentSmugLoopTagFunction(G g)
	{
		if (Instance.Api.IsOversmug(g.state, g.state.ship))
			return Instance.OversmugPortraitAnimation.Tag;
		var smug = Instance.Api.GetSmug(g.state, g.state.ship) ?? 0;
		return Instance.SmugPortraitAnimations[smug].Tag;
	}
}