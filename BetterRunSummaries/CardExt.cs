using Nickel;

namespace Shockah.BetterRunSummaries;

internal static class CardExt
{
	public static int? GetTimesPlayed(this Card card)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(card, "TimesPlayed");

	public static void SetTimesPlayed(this Card card, int? value)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(card, "TimesPlayed", value);

	public static void IncrementTimesPlayed(this Card card)
		=> card.SetTimesPlayed((card.GetTimesPlayed() ?? 0) + 1);
}