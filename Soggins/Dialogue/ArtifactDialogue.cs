using System;

namespace Shockah.Soggins;

internal static class ArtifactDialogue
{
	internal static void Inject()
	{
		foreach (var artifactType in ModEntry.AllArtifacts)
		{
			if (Activator.CreateInstance(artifactType) is not IRegisterableArtifact artifact)
				continue;
			artifact.InjectDialogue();
		}
	}
}
