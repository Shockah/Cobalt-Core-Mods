using Shockah.Shared;

namespace Shockah.Kokoro;

public sealed class ArtifactIconManager : HookManager<IArtifactIconHook>
{
	internal ArtifactIconManager()
	{
	}

	internal void OnRenderArtifactIcon(G g, Artifact artifact, Vec position)
	{
		foreach (var hook in GetHooksWithProxies(ModEntry.Instance.Api, g.state.EnumerateAllArtifacts()))
			hook.OnRenderArtifactIcon(g, artifact, position);
	}
}