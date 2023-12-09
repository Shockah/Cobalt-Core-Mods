namespace Shockah.Kokoro;

public sealed class ArtifactIconManager : HookManager<IArtifactIconHook>
{
	internal ArtifactIconManager() : base()
	{
	}

	internal void OnRenderArtifactIcon(G g, Artifact artifact, Vec position)
	{
		foreach (var hook in Hooks)
			hook.OnRenderArtifactIcon(g, artifact, position);
	}
}