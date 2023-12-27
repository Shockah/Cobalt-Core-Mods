using System.Collections.Generic;

namespace Shockah.Soggins;

public sealed class AEnableSmug : CardAction
{
	private static ModEntry Instance => ModEntry.Instance;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		Instance.Api.SetSmugEnabled(s, s.ship);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		var tooltips = base.GetTooltips(s);
		tooltips.Add(new TTGlossary($"status.{Instance.SmugStatus.Id!.Value}"));
		return tooltips;
	}
}