using Nickel;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.BetterRunSummaries;

internal static class CardSummaryExt
{
	public static IReadOnlyDictionary<ICardTraitEntry, bool> GetTraitOverrides(this CardSummary summary)
		=> ModEntry.Instance.Helper.ModData.GetModDataOrDefault<Dictionary<string, bool>>(summary, "TraitOverrides")
			.Select(kvp => new KeyValuePair<ICardTraitEntry?, bool>(ModEntry.Instance.Helper.Content.Cards.LookupTraitByUniqueName(kvp.Key), kvp.Value))
			.Where(kvp => kvp.Key is not null)
			.Select(kvp => new KeyValuePair<ICardTraitEntry, bool>(kvp.Key!, kvp.Value))
			.ToDictionary();

	public static void SetTraitOverrides(this CardSummary summary, IEnumerable<KeyValuePair<ICardTraitEntry, bool>> overrides)
	{
		var list = overrides.ToList();
		if (list.Count == 0)
		{
			ModEntry.Instance.Helper.ModData.RemoveModData(summary, "TraitOverrides");
			return;
		}
		ModEntry.Instance.Helper.ModData.SetModData(summary, "TraitOverrides", list.ToDictionary(kvp => kvp.Key.UniqueName, kvp => kvp.Value));
	}
}