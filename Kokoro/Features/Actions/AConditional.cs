using System.Collections.Generic;

namespace Shockah.Kokoro;

public sealed class AConditional : CardAction, ICardActionWrapper
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

		c.QueueImmediate(Action);
	}

	public override List<Tooltip> GetTooltips(State s)
	{
		List<Tooltip> tooltips = new();
		if (Expression is not null)
		{
			var description = Expression.GetTooltipDescription(s, s.route as Combat);

			tooltips.Add(new CustomTTGlossary(
				CustomTTGlossary.GlossaryType.action,
				() => (Spr)ModEntry.Instance.Content.QuestionMarkSprite.Id!.Value,
				() => I18n.ConditionalActionName,
				() => description,
				key: description
			));
			tooltips.AddRange(Expression.GetTooltips(s, s.route as Combat));
		}
		if (Action is not null)
			tooltips.AddRange(Action.GetTooltips(s));
		return tooltips;
	}

	public IEnumerable<CardAction> GetWrappedCardActions()
	{
		if (Action is not null)
			yield return Action;
	}
}