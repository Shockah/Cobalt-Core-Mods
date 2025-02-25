namespace Shockah.CodexHelper;

public sealed class ApiImplementation : ICodexHelperApi
{
	public ICodexHelperApi.ICardProgress GetCardProgress(State state, string key, CardReward? route = null)
	{
		if (state.persistentStoryVars.cardsOwned.Contains(key))
			return ICodexHelperApi.ICardProgress.Taken;
		if (state.persistentStoryVars.IsCardSeen(key, route))
			return ICodexHelperApi.ICardProgress.Seen;
		return ICodexHelperApi.ICardProgress.NotSeen;
	}
	
	public ICodexHelperApi.IArtifactProgress GetArtifactProgress(State state, string key, ArtifactReward? route = null)
	{
		if (state.persistentStoryVars.artifactsOwned.Contains(key))
			return ICodexHelperApi.IArtifactProgress.Taken;
		if (state.persistentStoryVars.IsArtifactSeen(key, route))
			return ICodexHelperApi.IArtifactProgress.Seen;
		return ICodexHelperApi.IArtifactProgress.NotSeen;
	}
}