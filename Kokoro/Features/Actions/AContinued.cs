using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class AContinued : CardAction
{
	private static ModEntry Instance => ModEntry.Instance;

	public Guid Id;
	public bool Continue;
	public CardAction? Action;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Action is null)
			return;

		var continueFlags = Instance.Api.ObtainExtensionData(c, Continue ? "ContinueFlags" : "StopFlags", () => new HashSet<Guid>());
		bool hasFlag = continueFlags.Contains(Id);

		if (Continue == !hasFlag)
			return;
		c.QueueImmediate(Action);
	}

	public override List<Tooltip> GetTooltips(State s)
		=> Action?.GetTooltips(s) ?? new();
}