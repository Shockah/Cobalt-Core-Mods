using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IConditionalApi Conditional { get; }

		public interface IConditionalApi
		{
			IConstantIntExpression? AsConstant(IExpression expression);
			IConstantIntExpression Constant(int value);
			
			IHandConstantIntExpression? AsHandConstant(IExpression expression);
			IHandConstantIntExpression HandConstant(int currentValue);
			
			IXConstantIntExpression? AsXConstant(IExpression expression);
			IXConstantIntExpression XConstant(int currentValue);
			
			IMultiplyIntExpression? AsMultiply(IExpression expression);
			IMultiplyIntExpression Multiply(IIntExpression expression, int scalar);
			
			IHasStatusExpression? AsHasStatus(IExpression expression);
			IHasStatusExpression HasStatus(Status status, bool targetPlayer = true);
			
			IStatusExpression? AsStatus(IExpression expression);
			IStatusExpression Status(Status status, bool targetPlayer = true);
			
			IEquation? AsEquation(IExpression expression);
			IEquation Equation(IIntExpression lhs, EquationOperator @operator, IIntExpression rhs, EquationStyle style);

			IConditionalAction? AsAction(CardAction action);
			IConditionalAction MakeAction(IBoolExpression expression, CardAction action);

			public enum EquationOperator
			{
				Equal, NotEqual, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual
			}

			public enum EquationStyle
			{
				Formal,
				State, Possession, PossessionComparison,
				EnemyState, EnemyPossession, EnemyPossessionComparison,
				ToEnemyPossessionComparison
			}

			public interface IExpression
			{
				void Render(G g, ref Vec position, bool isDisabled, bool dontRender);
				string GetTooltipDescription(State state, Combat? combat);
				List<Tooltip> GetTooltips(State state, Combat? combat) => [];
			}

			public interface IBoolExpression : IExpression
			{
				bool GetValue(State state, Combat combat);
				bool ShouldRenderQuestionMark(State state, Combat? combat) => true;
				IEnumerable<Tooltip> OverrideConditionalTooltip(State state, Combat? combat, Tooltip defaultTooltip, string defaultTooltipDescription) => [defaultTooltip];
			}

			public interface IIntExpression : IExpression
			{
				int GetValue(State state, Combat combat);
			}

			public interface IConstantIntExpression : IIntExpression
			{
				int Value { get; set; }

				IConstantIntExpression SetValue(int value);
			}

			public interface IHandConstantIntExpression : IIntExpression
			{
				int CurrentValue { get; set; }

				IHandConstantIntExpression SetCurrentValue(int value);
			}

			public interface IXConstantIntExpression : IIntExpression
			{
				int CurrentValue { get; set; }

				IXConstantIntExpression SetCurrentValue(int value);
			}

			public interface IMultiplyIntExpression : IIntExpression
			{
				IIntExpression Expression { get; set; }
				int Scalar { get; set; }

				IMultiplyIntExpression SetExpression(IIntExpression value);
				IMultiplyIntExpression SetScalar(int value);
			}

			public interface IHasStatusExpression : IBoolExpression
			{
				Status Status { get; set; }
				bool TargetPlayer { get; set; }
				bool CountNegative { get; set; }

				IHasStatusExpression SetStatus(Status value);
				IHasStatusExpression SetTargetPlayer(bool value);
				IHasStatusExpression SetCountNegative(bool value);
			}

			public interface IStatusExpression : IIntExpression
			{
				Status Status { get; set; }
				bool TargetPlayer { get; set; }

				IStatusExpression SetStatus(Status value);
				IStatusExpression SetTargetPlayer(bool value);
			}

			public interface IEquation : IBoolExpression
			{
				IIntExpression Lhs { get; set; }
				EquationOperator Operator { get; set; }
				IIntExpression Rhs { get; set; }
				EquationStyle Style { get; set; }
				bool HideOperator { get; set; }

				IEquation SetLhs(IIntExpression value);
				IEquation SetOperator(EquationOperator value);
				IEquation SetRhs(IIntExpression value);
				IEquation SetStyle(EquationStyle value);
				IEquation SetHideOperator(bool value);
			}

			public interface IConditionalAction : ICardAction<CardAction>
			{
				IBoolExpression Expression { get; set; }
				CardAction Action { get; set; }
				bool FadeUnsatisfied { get; set; }
				
				IConditionalAction SetExpression(IBoolExpression value);
				IConditionalAction SetAction(CardAction value);
				IConditionalAction SetFadeUnsatisfied(bool value);
			}
		}
	}
}
