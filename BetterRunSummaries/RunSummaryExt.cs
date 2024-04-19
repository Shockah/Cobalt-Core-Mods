using Nickel;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.BetterRunSummaries;

internal static class RunSummaryExt
{
	public static IReadOnlyDictionary<string, int> GetTimesArtifactsTriggered(this RunSummary summary)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<Dictionary<string, int>>(summary, "TimesArtifactsTriggered") ?? [];

	public static void SetTimesArtifactsTriggered(this RunSummary summary, IEnumerable<KeyValuePair<string, int>> times)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(summary, "TimesArtifactsTriggered", times.ToDictionary());
}