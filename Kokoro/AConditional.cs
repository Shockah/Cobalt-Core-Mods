using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class AConditional : CardAction
{
	public IConditionalActionBoolExpression? Expression;
	public CardAction? Action;

	private bool WasSatisfied = false;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Expression is null || Action is null)
			return;
		if (!Expression.GetValue(s, c))
			return;

		WasSatisfied = true;
		Action.Begin(g, s, c);
		timer = Action.timer;
	}

	public override void Update(G g, State s, Combat c)
	{
		base.Update(g, s, c);
		if (!WasSatisfied || Action is null)
			return;

		Action.Update(g, s, c);
		timer = Action.timer;
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		List<Tooltip> tooltips = new();
		if (Expression is not null)
			tooltips.AddRange(Expression.GetTooltips(s, s.route as Combat));
		if (Action is not null)
			tooltips.AddRange(Action.GetTooltips(s));
		return tooltips;
	}
}