﻿using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class AHidden : CardAction
{
	public CardAction? Action;
	public bool ShowTooltips = false;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Action is null)
			return;
		Action.whoDidThis = whoDidThis;
		c.QueueImmediate(Action);
	}

	public override List<Tooltip> GetTooltips(State s)
		=> (!ShowTooltips || Action?.omitFromTooltips == true) ? [] : (Action?.GetTooltips(s) ?? []);

	public override bool CanSkipTimerIfLastEvent()
		=> Action?.CanSkipTimerIfLastEvent() ?? base.CanSkipTimerIfLastEvent();
}