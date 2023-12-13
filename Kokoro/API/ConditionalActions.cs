using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IConditionalActionIntExpression MakeConditionalActionIntConstant(int value);
	IConditionalActionIntExpression MakeConditionalActionHandConstant(int value);
	IConditionalActionIntExpression MakeConditionalActionXConstant(int value);
	IConditionalActionIntExpression MakeConditionalActionScalarMultiplier(IConditionalActionIntExpression expression, int scalar);
	IConditionalActionBoolExpression MakeConditionalActionHasStatusExpression(Status status, bool targetPlayer = true, bool countNegative = false);
	IConditionalActionIntExpression MakeConditionalActionStatusExpression(Status status, bool targetPlayer = true);
	IConditionalActionBoolExpression MakeConditionalActionEquation(IConditionalActionIntExpression lhs, ConditionalActionEquationOperator @operator, IConditionalActionIntExpression rhs);
	CardAction MakeConditionalAction(IConditionalActionBoolExpression expression, CardAction action);
}

public enum ConditionalActionEquationOperator
{
	Equal, NotEqual, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual
}

public interface IConditionalActionExpression
{
	void Render(G g, ref Vec position, bool isDisabled, bool dontRender);
	List<Tooltip> GetTooltips(State state, Combat? combat) => new();
}

public interface IConditionalActionBoolExpression : IConditionalActionExpression
{
	bool GetValue(State state, Combat combat);
}

public interface IConditionalActionIntExpression : IConditionalActionExpression
{
	int GetValue(State state, Combat combat);
}