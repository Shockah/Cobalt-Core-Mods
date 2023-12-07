using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

internal sealed class AHiddenStatus : AStatus
{
	public AHiddenStatus()
	{
		this.omitFromTooltips = true;
	}

	public override List<Tooltip> GetTooltips(State s)
		=> new();

	public override Icon? GetIcon(State s)
		=> null;
}
