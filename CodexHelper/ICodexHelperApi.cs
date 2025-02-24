namespace Shockah.CodexHelper;

public interface ICodexHelperApi
{
	ICardProgress GetCardProgress(State state, string key, CardReward? route = null);
	
	public enum ICardProgress
	{
		NotSeen, Seen, Taken
	}
}