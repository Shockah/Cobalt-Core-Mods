using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

internal sealed class ConditionalActionIntConstant : IConditionalActionIntExpression
{
	[JsonProperty]
	private readonly int Value;

	[JsonConstructor]
	public ConditionalActionIntConstant(int value)
	{
		this.Value = value;
	}

	public int GetValue(State state, Combat combat)
		=> Value;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			BigNumbers.Render(Value, position.x, position.y, isDisabled ? Colors.disabledText : Colors.textMain);
		position.x += $"{Value}".Length * 6;
	}
}

internal sealed class ConditionalActionHandConstant : IConditionalActionIntExpression
{
	[JsonProperty]
	private readonly int Value;

	[JsonConstructor]
	public ConditionalActionHandConstant(int value)
	{
		this.Value = value;
	}

	public int GetValue(State state, Combat combat)
		=> Value;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(StableSpr.icons_dest_hand, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}
}

internal sealed class ConditionalActionXConstant : IConditionalActionIntExpression
{
	[JsonProperty]
	private readonly int Value;

	[JsonConstructor]
	public ConditionalActionXConstant(int value)
	{
		this.Value = value;
	}

	public int GetValue(State state, Combat combat)
		=> Value;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!dontRender)
			Draw.Sprite(StableSpr.icons_x, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}
}

internal sealed class ConditionalActionScalarMultiplier : IConditionalActionIntExpression
{
	[JsonProperty]
	private readonly IConditionalActionIntExpression Expression;

	[JsonProperty]
	private readonly int Scalar;

	[JsonConstructor]
	public ConditionalActionScalarMultiplier(IConditionalActionIntExpression expression, int scalar)
	{
		this.Expression = expression;
		this.Scalar = scalar;
	}

	public int GetValue(State state, Combat combat)
		=> Expression.GetValue(state, combat) * Scalar;

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (Expression is ConditionalActionIntConstant constant)
		{
			new ConditionalActionIntConstant(constant.GetValue(g.state, DB.fakeCombat) * Scalar).Render(g, ref position, isDisabled, dontRender);
			return;
		}

		if (!dontRender)
			BigNumbers.Render(Scalar, position.x, position.y, isDisabled ? Colors.disabledText : Colors.textMain);
		position.x += $"{Scalar}".Length * 6;
		Expression.Render(g, ref position, isDisabled, dontRender);
	}
}

internal sealed class ConditionalActionHasStatusExpression : IConditionalActionBoolExpression
{
	[JsonProperty]
	private readonly Status Status;

	[JsonProperty]
	private readonly bool TargetPlayer;

	[JsonProperty]
	private readonly bool CountNegative;

	[JsonConstructor]
	public ConditionalActionHasStatusExpression(Status status, bool targetPlayer, bool countNegative)
	{
		this.Status = status;
		this.TargetPlayer = targetPlayer;
		this.CountNegative = countNegative;
	}

	public bool GetValue(State state, Combat combat)
	{
		var ship = TargetPlayer ? state.ship : combat.otherShip;
		var amount = ship.Get(Status);
		return CountNegative ? amount != 0 : amount > 0;
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!TargetPlayer)
		{
			if (!dontRender)
				Draw.Sprite(StableSpr.icons_outgoing, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += 8;
		}

		var icon = DB.statuses[Status].icon;
		if (!dontRender)
			Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public List<Tooltip> GetTooltips(State state, Combat? combat)
	{
		var ship = TargetPlayer ? state.ship : combat?.otherShip;
		var amount = ship?.Get(Status) ?? 1;
		return new() { new TTGlossary($"status.{Status.Key()}", amount) };
	}
}

internal sealed class ConditionalActionStatusExpression : IConditionalActionIntExpression
{
	[JsonProperty]
	private readonly Status Status;

	[JsonProperty]
	private readonly bool TargetPlayer;

	[JsonConstructor]
	public ConditionalActionStatusExpression(Status status, bool targetPlayer)
	{
		this.Status = status;
		this.TargetPlayer = targetPlayer;
	}

	public int GetValue(State state, Combat combat)
	{
		var ship = TargetPlayer ? state.ship : combat.otherShip;
		return ship.Get(Status);
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		if (!TargetPlayer)
		{
			if (!dontRender)
				Draw.Sprite(StableSpr.icons_outgoing, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += 8;
		}

		var icon = DB.statuses[Status].icon;
		if (!dontRender)
			Draw.Sprite(icon, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
		position.x += 8;
	}

	public List<Tooltip> GetTooltips(State state, Combat? combat)
	{
		var ship = TargetPlayer ? state.ship : combat?.otherShip;
		var amount = ship?.Get(Status) ?? 1;
		return new() { new TTGlossary($"status.{Status.Key()}", amount) };
	}
}

internal sealed class ConditionalActionEquation : IConditionalActionBoolExpression
{
	[JsonProperty]
	private readonly IConditionalActionIntExpression Lhs;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	private readonly ConditionalActionEquationOperator Operator;

	[JsonProperty]
	private readonly IConditionalActionIntExpression Rhs;

	[JsonProperty]
	private readonly bool HideOperator;

	[JsonConstructor]
	public ConditionalActionEquation(IConditionalActionIntExpression lhs, ConditionalActionEquationOperator @operator, IConditionalActionIntExpression rhs, bool hideOperator)
	{
		this.Lhs = lhs;
		this.Operator = @operator;
		this.Rhs = rhs;
		this.HideOperator = hideOperator;
	}

	public bool GetValue(State state, Combat combat)
	{
		int lhs = Lhs.GetValue(state, combat);
		int rhs = Rhs.GetValue(state, combat);
		return Operator switch
		{
			ConditionalActionEquationOperator.Equal => lhs == rhs,
			ConditionalActionEquationOperator.NotEqual => lhs != rhs,
			ConditionalActionEquationOperator.GreaterThan => lhs > rhs,
			ConditionalActionEquationOperator.LessThan => lhs < rhs,
			ConditionalActionEquationOperator.GreaterThanOrEqual => lhs >= rhs,
			ConditionalActionEquationOperator.LessThanOrEqual => lhs <= rhs,
			_ => throw new ArgumentException()
		};
	}

	public void Render(G g, ref Vec position, bool isDisabled, bool dontRender)
	{
		Lhs.Render(g, ref position, isDisabled, dontRender);

		if (!HideOperator)
		{
			var operatorIcon = Operator switch
			{
				ConditionalActionEquationOperator.Equal => ModEntry.Instance.Content.EqualSprite,
				ConditionalActionEquationOperator.NotEqual => ModEntry.Instance.Content.NotEqualSprite,
				ConditionalActionEquationOperator.GreaterThan => ModEntry.Instance.Content.GreaterThanSprite,
				ConditionalActionEquationOperator.LessThan => ModEntry.Instance.Content.LessThanSprite,
				ConditionalActionEquationOperator.GreaterThanOrEqual => ModEntry.Instance.Content.GreaterThanOrEqualSprite,
				ConditionalActionEquationOperator.LessThanOrEqual => ModEntry.Instance.Content.LessThanOrEqualSprite,
				_ => throw new ArgumentException()
			};
			if (!dontRender)
				Draw.Sprite((Spr)operatorIcon.Id!.Value, position.x, position.y, color: isDisabled ? Colors.disabledIconTint : Colors.white);
			position.x += SpriteLoader.Get((Spr)operatorIcon.Id!.Value)?.Width ?? 0;
		}

		Rhs.Render(g, ref position, isDisabled, dontRender);
	}

	public List<Tooltip> GetTooltips(State state, Combat? combat)
		=> Lhs.GetTooltips(state, combat).Concat(Rhs.GetTooltips(state, combat)).ToList();
}