namespace Shockah.Kokoro;

public sealed class ArtifactIconHookManager : HookManager<IArtifactIconHook>
{
	internal ArtifactIconHookManager() : base()
	{
	}

	internal void OnRenderArtifactIcon(G g, Artifact artifact, Vec position)
	{
		foreach (var hook in Hooks)
			hook.OnRenderArtifactIcon(g, artifact, position);
	}
}