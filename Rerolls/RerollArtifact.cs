using System.Collections.Generic;

namespace Shockah.Rerolls;

[ArtifactMeta(unremovable = true)]
internal sealed class RerollArtifact : Artifact
{
	public int RerollsLeft = 1;

	public override int? GetDisplayNumber(State s)
		=> RerollsLeft;

	public override List<Tooltip>? GetExtraTooltips()
	{
		var tooltips = base.GetExtraTooltips() ?? new();
		tooltips.Add(new TTText(I18n.GetArtifactCountTooltip(RerollsLeft)));
		return tooltips;
	}
}