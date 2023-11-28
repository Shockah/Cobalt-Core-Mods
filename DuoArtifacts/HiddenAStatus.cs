using System.Collections.Generic;

namespace Shockah.DuoArtifacts;

internal sealed class HiddenAStatus : AStatus
{
	public override List<Tooltip> GetTooltips(State s)
		=> new();

	public override Icon? GetIcon(State s)
		=> null;
}
