namespace Shockah.CustomRunOptions;

public sealed class ArtifactNewRunOptionsElement(Artifact artifact) : IconNewRunOptionsElement(artifact.GetSprite(), 13, 13), ICustomRunOptionsApi.INewRunOptionsElement.IArtifact
{
	private Artifact ArtifactStorage = artifact;

	public Artifact Artifact
	{
		get => ArtifactStorage;
		set
		{
			ArtifactStorage = value;
			Icon = value.GetSprite();
		}
	}
	
	public ICustomRunOptionsApi.INewRunOptionsElement.IArtifact SetArtifact(Artifact value)
	{
		this.Artifact = value;
		return this;
	}
}