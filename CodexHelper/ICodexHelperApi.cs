namespace Shockah.CodexHelper;

public interface ICodexHelperApi
{
	ICardProgress GetCardProgress(State state, string key, CardReward? route = null);
	IArtifactProgress GetArtifactProgress(State state, string key, ArtifactReward? route = null);
	
	public enum ICardProgress
	{
		NotSeen, Seen, Taken
	}
	
	public enum IArtifactProgress
	{
		NotSeen, Seen, Taken
	}
}