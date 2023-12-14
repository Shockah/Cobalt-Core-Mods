using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	IConditionalActionApi ConditionalActions { get; }
	
	public partial interface IConditionalActionApi
	{
		CardAction Make(IBoolExpression expression, CardAction action, bool fadeUnsatisfied = true);
		IIntExpression Constant(int value);
		IIntExpression HandConstant(int value);
		IIntExpression XConstant(int value);
		IIntExpression ScalarMultiplier(IIntExpression expression, int scalar);
		IBoolExpression HasStatus(Status status, bool targetPlayer = true, bool countNegative = false);
		IIntExpression Status(Status status, bool targetPlayer = true);
		IBoolExpression Equation(IIntExpression lhs, EquationOperator @operator, IIntExpression rhs, bool hideOperator = false);

		public enum EquationOperator
		{
			Equal, NotEqual, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual
		}

		public interface IExpression
		{
			void Render(G g, ref Vec position, bool isDisabled, bool dontRender);
			List<Tooltip> GetTooltips(State state, Combat? combat) => new();
		}

		public interface IBoolExpression : IExpression
		{
			bool GetValue(State state, Combat combat);
			bool ShouldRenderQuestionMark(State state, Combat? combat) => true;
		}

		public interface IIntExpression : IExpression
		{
			int GetValue(State state, Combat combat);
		}
	}
}