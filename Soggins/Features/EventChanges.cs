using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace Shockah.Soggins;

internal static class EventChangesManager
{
	public static void Inject()
	{
		var choiceFuncName = $"{ModEntry.Instance.Name}::{nameof(EventChangesManager)}::SogginsDiedIGuess";

		DB.story.all["SogginsDiedIGuess_1"].choiceFunc = choiceFuncName;
		DB.eventChoiceFns[choiceFuncName] = AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetChoices));
	}

	[UsedImplicitly]
	private static List<Choice> GetChoices(State state)
	{
		var choices = new List<Choice>();
		var artifacts = new List<Artifact>();
		
		if (!state.EnumerateAllArtifacts().Any(a => a is SmugArtifact))
			artifacts.Add(new SmugArtifact());
		if (!state.EnumerateAllArtifacts().Any(a => a is VideoWillArtifact))
			artifacts.Add(new VideoWillArtifact());

		if (artifacts.Count != 0)
			choices.Add(new()
			{
				label = "In memory of Soggins...",
				actions =
				[
					new AAddArtifact { artifact = new SmugArtifact() },
					new AAddArtifact { artifact = new VideoWillArtifact() },
				],
			});

		choices.Add(new()
		{
			label = "Let's.",
		});

		return choices;
	}
}