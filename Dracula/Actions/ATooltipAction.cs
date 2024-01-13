using Newtonsoft.Json;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class ATooltipAction : ADummyAction
{
	[JsonProperty]
	public List<Tooltip>? Tooltips;

	public override List<Tooltip> GetTooltips(State s)
		=> Tooltips ?? [];
}
