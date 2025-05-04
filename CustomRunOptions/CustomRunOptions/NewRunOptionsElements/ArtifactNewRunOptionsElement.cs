namespace Shockah.CustomRunOptions;

internal sealed class ArtifactNewRunOptionsElement(Artifact artifact) : ICustomRunOption.INewRunOptionsElement
{
	public Vec Size
		=> new(13, 13);

	public void Render(G g, Vec position)
		=> Draw.Sprite(artifact.GetSprite(), position.x, position.y);
}