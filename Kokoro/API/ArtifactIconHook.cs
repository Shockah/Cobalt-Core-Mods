namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	void RegisterArtifactIconHook(IArtifactIconHook hook, double priority);
	void UnregisterArtifactIconHook(IArtifactIconHook hook);
}

public interface IArtifactIconHook
{
	void OnRenderArtifactIcon(G g, Artifact artifact, Vec position);
}