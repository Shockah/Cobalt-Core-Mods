namespace Shockah.CodexHelper;

public sealed class ApiImplementation : ICodexHelperApi
{
	public ICodexHelperApi.ICardProgress GetCardProgress(State state, string key, CardReward? route = null)
	{
		if (state.persistentStoryVars.cardsOwned.Contains(key))
			return ICodexHelperApi.ICardProgress.Taken;
		if (state.persistentStoryVars.IsSeen(key, route))
			return ICodexHelperApi.ICardProgress.Seen;
		return ICodexHelperApi.ICardProgress.NotSeen;
	}
}