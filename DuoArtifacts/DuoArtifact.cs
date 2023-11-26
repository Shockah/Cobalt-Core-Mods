using HarmonyLib;

namespace Shockah.DuoArtifacts;

[ArtifactMeta(pools = new ArtifactPool[] { ArtifactPool.Boss })]
public abstract class DuoArtifact : Artifact
{
	protected internal virtual void ApplyPatches(Harmony harmony)
	{
	}
}
