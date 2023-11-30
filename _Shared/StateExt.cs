using Shockah.Shared;

internal static class StateExt
{
	public static State? Instance
		=> GExt.Instance?.state;
}