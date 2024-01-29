using Newtonsoft.Json;
using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class ATooltipAction : ADummyAction
{
	[JsonProperty]
	public List<Tooltip>? Tooltips;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
	}

	public override List<Tooltip> GetTooltips(State s)
		=> Tooltips ?? [];
}
