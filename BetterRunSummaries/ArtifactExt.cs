using Nickel;

namespace Shockah.BetterRunSummaries;

internal static class ArtifactExt
{
	public static int? GetTimesTriggered(this Artifact artifact)
		=> ModEntry.Instance.Helper.ModData.GetOptionalModData<int>(artifact, "TimesTriggered");

	public static void SetTimesTriggered(this Artifact artifact, int? value)
		=> ModEntry.Instance.Helper.ModData.SetOptionalModData(artifact, "TimesTriggered", value);

	public static void IncrementTimesTriggered(this Artifact artifact)
		=> artifact.SetTimesTriggered((artifact.GetTimesTriggered() ?? 0) + 1);
}