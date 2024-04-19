using Nickel;

namespace Shockah.BetterRunSummaries;

internal static class CardExt
{
	public static int? GetTimesPlayed(this Card Card)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(Card, "TimesPlayed");

	public static void SetTimesPlayed(this Card Card, int? value)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(Card, "TimesPlayed", value);

	public static void IncrementTimesPlayed(this Card card)
		=> card.SetTimesPlayed((card.GetTimesPlayed() ?? 0) + 1);
}