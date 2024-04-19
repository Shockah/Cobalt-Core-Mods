using Nickel;
using System.Collections.Generic;

namespace Shockah.BetterRunSummaries;

internal static class CardExt
{
	public static int? GetTimesPlayed(this Card card, State state)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<Dictionary<int, int>>(state, "TimesCardsPlayed")?.TryGetValue(card.uuid, out var timesPlayed) == true ? timesPlayed : null;

	public static void SetTimesPlayed(this Card card, State state, int? value)
	{
		var counts = ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<int, int>>(state, "TimesCardsPlayed");
		if (value is { } nonNullValue)
			counts[card.uuid] = nonNullValue;
		else
			counts.Remove(card.uuid);
	}

	public static void IncrementTimesPlayed(this Card card, State state)
		=> card.SetTimesPlayed(state, (card.GetTimesPlayed(state) ?? 0) + 1);
}