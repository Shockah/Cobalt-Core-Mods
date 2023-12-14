using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

internal sealed class ConditionalActionIntConstant : IKokoroApi.IConditionalActionApi.IIntExpression
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

internal sealed class ConditionalActionHandConstant : IKokoroApi.IConditionalActionApi.IIntExpression
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

internal sealed class ConditionalActionXConstant : IKokoroApi.IConditionalActionApi.IIntExpression
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

internal sealed class ConditionalActionScalarMultiplier : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	private readonly IKokoroApi.IConditionalActionApi.IIntExpression Expression;

	[JsonProperty]
	private readonly int Scalar;

	[JsonConstructor]
	public ConditionalActionScalarMultiplier(IKokoroApi.IConditionalActionApi.IIntExpression expression, int scalar)
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

internal sealed class ConditionalActionHasStatusExpression : IKokoroApi.IConditionalActionApi.IBoolExpression
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

internal sealed class ConditionalActionStatusExpression : IKokoroApi.IConditionalActionApi.IIntExpression
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

internal sealed class ConditionalActionEquation : IKokoroApi.IConditionalActionApi.IBoolExpression
{
	[JsonProperty]
	private readonly IKokoroApi.IConditionalActionApi.IIntExpression Lhs;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	private readonly IKokoroApi.IConditionalActionApi.EquationOperator Operator;

	[JsonProperty]
	private readonly IKokoroApi.IConditionalActionApi.IIntExpression Rhs;

	[JsonProperty]
	private readonly bool HideOperator;

	[JsonConstructor]
	public ConditionalActionEquation(IKokoroApi.IConditionalActionApi.IIntExpression lhs, IKokoroApi.IConditionalActionApi.EquationOperator @operator, IKokoroApi.IConditionalActionApi.IIntExpression rhs, bool hideOperator)
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
			IKokoroApi.IConditionalActionApi.EquationOperator.Equal => lhs == rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.NotEqual => lhs != rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThan => lhs > rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.LessThan => lhs < rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual => lhs >= rhs,
			IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual => lhs <= rhs,
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
				IKokoroApi.IConditionalActionApi.EquationOperator.Equal => ModEntry.Instance.Content.EqualSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.NotEqual => ModEntry.Instance.Content.NotEqualSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThan => ModEntry.Instance.Content.GreaterThanSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThan => ModEntry.Instance.Content.LessThanSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual => ModEntry.Instance.Content.GreaterThanOrEqualSprite,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual => ModEntry.Instance.Content.LessThanOrEqualSprite,
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