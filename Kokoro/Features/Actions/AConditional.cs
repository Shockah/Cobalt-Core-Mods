using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class AConditional : CardAction
{
	public IKokoroApi.IConditionalActionApi.IBoolExpression? Expression;
	public CardAction? Action;
	public bool FadeUnsatisfied = true;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		if (Expression is null || Action is null)
			return;
		if (!Expression.GetValue(s, c))
			return;

		Action.whoDidThis = whoDidThis;
		c.QueueImmediate(Action);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		List<Tooltip> tooltips = [];
		if (Expression is not null)
		{
			var description = Expression.GetTooltipDescription(s, s.route as Combat);
			var formattedDescription = string.Format(I18n.ConditionalActionDescription, description);
			tooltips.Add(new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => (Spr)ModEntry.Instance.Content.QuestionMarkSprite.Id!.Value,
				() => I18n.ConditionalActionName,
				() => formattedDescription,
				key: $"AConditional::{formattedDescription}"
			));
			tooltips.AddRange(Expression.GetTooltips(s, s.route as Combat));
		}
		if (Action is not null && !Action.omitFromTooltips)
			tooltips.AddRange(Action.GetTooltips(s));
		return tooltips;
	}
}