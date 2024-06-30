using System.Collections.Generic;

namespace Shockah.Rerolls;

internal sealed class RerollArtifact : Artifact
{
	public int RerollsLeft = ModEntry.Instance.Settings.ProfileBased.Current.InitialRerolls;

	public override int? GetDisplayNumber(State s)
		=> RerollsLeft;

	public override List<Tooltip>? GetExtraTooltips()
		=> [
			..base.GetExtraTooltips() ?? [],
			new TTText(ModEntry.Instance.Localizations.Localize(["artifact", "extraDescription", RerollsLeft switch
			{
				0 => "zero",
				1 => "one",
				_ => "other"
			}], new { Amount = RerollsLeft }))
		];
}