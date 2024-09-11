using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class ASpoofed : CardAction
{
	public CardAction? RenderAction;
	public CardAction? RealAction;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (RealAction is null)
			return;
		RealAction.whoDidThis = whoDidThis;
		c.QueueImmediate(RealAction);
	}

	public override Icon? GetIcon(State s)
		=> RenderAction?.GetIcon(s);

	public override List<Tooltip> GetTooltips(State s)
		=> RenderAction?.omitFromTooltips == true ? [] : (RenderAction?.GetTooltips(s) ?? []);

	public override bool CanSkipTimerIfLastEvent()
		=> RealAction?.CanSkipTimerIfLastEvent() ?? base.CanSkipTimerIfLastEvent();
}