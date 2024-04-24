using Nickel;

namespace Shockah.BetterRunSummaries;

internal static class StateExt
{
	public static string? GetEnemyDiedTo(this State state)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<string>(state, "EnemyDiedTo");

	public static void SetEnemyDiedTo(this State state, string? value)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(state, "EnemyDiedTo", value);
}