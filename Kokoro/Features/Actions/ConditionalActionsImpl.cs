using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Shockah.Kokoro;

internal sealed class ConditionalActionIntConstant : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public readonly int Value;

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

	public string GetTooltipDescription(State state, Combat? combat)
		=> $"<c=boldPink>{Value}</c>";
}

internal sealed class ConditionalActionHandConstant : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public readonly int Value;

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

	public string GetTooltipDescription(State state, Combat? combat)
		=> string.Format(I18n.ConditionalHandDescription, Value);
}

internal sealed class ConditionalActionXConstant : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public readonly int Value;

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

	public string GetTooltipDescription(State state, Combat? combat)
		=> I18n.ConditionalXDescription;
}

internal sealed class ConditionalActionScalarMultiplier : IKokoroApi.IConditionalActionApi.IIntExpression
{
	[JsonProperty]
	public readonly IKokoroApi.IConditionalActionApi.IIntExpression Expression;

	[JsonProperty]
	public readonly int Scalar;

	[JsonIgnore]
	internal int? ConstantValue
	{
		get
		{
			if (Expression is ConditionalActionIntConstant constant)
				return constant.Value;
			if (Expression is ConditionalActionScalarMultiplier scalarMultiplier)
			{
				var scalarConstant = scalarMultiplier.ConstantValue;
				if (scalarConstant is not null)
					return scalarConstant.Value * Scalar;
			}
			return null;
		}
	}

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
		var constantValue = ConstantValue;
		if (constantValue is not null)
		{
			new ConditionalActionIntConstant(constantValue.Value).Render(g, ref position, isDisabled, dontRender);
			return;
		}

		if (!dontRender)
			BigNumbers.Render(Scalar, position.x, position.y, isDisabled ? Colors.disabledText : Colors.textMain);
		position.x += $"{Scalar}".Length * 6;
		Expression.Render(g, ref position, isDisabled, dontRender);
	}

	public string GetTooltipDescription(State state, Combat? combat)
	{
		var constantValue = ConstantValue;
		if (constantValue is not null)
			return new ConditionalActionIntConstant(constantValue.Value).GetTooltipDescription(state, combat);
		return $"<c=boldPink>{Scalar}</c>x {Expression.GetTooltipDescription(state, combat)}";
	}
}

internal sealed class ConditionalActionHasStatusExpression : IKokoroApi.IConditionalActionApi.IBoolExpression
{
	[JsonProperty]
	public readonly Status Status;

	[JsonProperty]
	public readonly bool TargetPlayer;

	[JsonProperty]
	public readonly bool CountNegative;

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

	public string GetTooltipDescription(State state, Combat? combat)
		=> string.Format(I18n.ConditionalHasStatusDescription, $"<c=status>{Status.GetLocName().ToUpper()}</c>");

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
	public readonly Status Status;

	[JsonProperty]
	public readonly bool TargetPlayer;

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

	public string GetTooltipDescription(State state, Combat? combat)
		=> $"<c=status>{Status.GetLocName().ToUpper()}</c>";

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
	public readonly IKokoroApi.IConditionalActionApi.IIntExpression Lhs;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public readonly IKokoroApi.IConditionalActionApi.EquationOperator Operator;

	[JsonProperty]
	public readonly IKokoroApi.IConditionalActionApi.IIntExpression Rhs;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public readonly IKokoroApi.IConditionalActionApi.EquationStyle Style;

	[JsonProperty]
	public readonly bool HideOperator;

	[JsonConstructor]
	public ConditionalActionEquation(
		IKokoroApi.IConditionalActionApi.IIntExpression lhs,
		IKokoroApi.IConditionalActionApi.EquationOperator @operator,
		IKokoroApi.IConditionalActionApi.IIntExpression rhs,
		IKokoroApi.IConditionalActionApi.EquationStyle style,
		bool hideOperator
	)
	{
		this.Lhs = lhs;
		this.Operator = @operator;
		this.Rhs = rhs;
		this.Style = style;
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

	private string GetTooltipDescriptionFormat()
		=> Style switch
		{
			IKokoroApi.IConditionalActionApi.EquationStyle.Formal => Operator switch
			{
				IKokoroApi.IConditionalActionApi.EquationOperator.Equal => I18n.ConditionalEquationFormalEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.NotEqual => I18n.ConditionalEquationFormalNotEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThan => I18n.ConditionalEquationFormalGreaterThanDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThan => I18n.ConditionalEquationFormalLessThanDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual => I18n.ConditionalEquationFormalGreaterThanOrEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual => I18n.ConditionalEquationFormalLessThanOrEqualDescription,
				_ => throw new ArgumentException()
			},
			IKokoroApi.IConditionalActionApi.EquationStyle.State => Operator switch
			{
				IKokoroApi.IConditionalActionApi.EquationOperator.Equal => I18n.ConditionalEquationStateEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.NotEqual => I18n.ConditionalEquationStateNotEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThan => I18n.ConditionalEquationStateGreaterThanDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThan => I18n.ConditionalEquationStateLessThanDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual => I18n.ConditionalEquationStateGreaterThanOrEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual => I18n.ConditionalEquationStateLessThanOrEqualDescription,
				_ => throw new ArgumentException()
			},
			IKokoroApi.IConditionalActionApi.EquationStyle.Possession => Operator switch
			{
				IKokoroApi.IConditionalActionApi.EquationOperator.Equal => I18n.ConditionalEquationPossessionEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.NotEqual => I18n.ConditionalEquationPossessionNotEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThan => I18n.ConditionalEquationPossessionGreaterThanDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThan => I18n.ConditionalEquationPossessionLessThanDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual => I18n.ConditionalEquationPossessionGreaterThanOrEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual => I18n.ConditionalEquationPossessionLessThanOrEqualDescription,
				_ => throw new ArgumentException()
			},
			IKokoroApi.IConditionalActionApi.EquationStyle.PossessionComparison => Operator switch
			{
				IKokoroApi.IConditionalActionApi.EquationOperator.Equal => I18n.ConditionalEquationPossessionComparisonEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.NotEqual => I18n.ConditionalEquationPossessionComparisonNotEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThan => I18n.ConditionalEquationPossessionComparisonGreaterThanDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThan => I18n.ConditionalEquationPossessionComparisonLessThanDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.GreaterThanOrEqual => I18n.ConditionalEquationPossessionComparisonGreaterThanOrEqualDescription,
				IKokoroApi.IConditionalActionApi.EquationOperator.LessThanOrEqual => I18n.ConditionalEquationPossessionComparisonLessThanOrEqualDescription,
				_ => throw new ArgumentException()
			},
			_ => throw new ArgumentException()
		};

	public string GetTooltipDescription(State state, Combat? combat)
		=> string.Format(GetTooltipDescriptionFormat(), Lhs.GetTooltipDescription(state, combat), Rhs.GetTooltipDescription(state, combat));

	public List<Tooltip> GetTooltips(State state, Combat? combat)
	{
		List<Tooltip> tooltips = new();
		tooltips.AddRange(Lhs.GetTooltips(state, combat));
		tooltips.AddRange(Rhs.GetTooltips(state, combat));
		return tooltips;
	}
}