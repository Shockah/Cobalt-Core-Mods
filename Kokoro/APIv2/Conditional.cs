using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IConditionalApi"/>
		IConditionalApi Conditional { get; }

		/// <summary>
		/// Allows working with and creating conditional actions.
		/// </summary>
		public interface IConditionalApi
		{
			/// <summary>
			/// Casts the expression to <see cref="IConstantIntExpression"/>, if it is one.
			/// </summary>
			/// <param name="expression">The expression.</param>
			/// <returns>The <see cref="IConstantIntExpression"/>, if the given expression is one, or <c>null</c> otherwise.</returns>
			IConstantIntExpression? AsConstant(IExpression expression);
			
			/// <summary>
			/// Creates a constant expression with the given value.
			/// </summary>
			/// <param name="value">The constant value.</param>
			/// <returns>A new constant expression.</returns>
			IConstantIntExpression Constant(int value);
			
			/// <summary>
			/// Casts the expression to <see cref="IHandConstantIntExpression"/>, if it is one.
			/// </summary>
			/// <param name="expression">The expression.</param>
			/// <returns>The <see cref="IHandConstantIntExpression"/>, if the given expression is one, or <c>null</c> otherwise.</returns>
			IHandConstantIntExpression? AsHandConstant(IExpression expression);
			
			/// <summary>
			/// Creates a constant expression with the given value, based on the current amount of cards in the player's hand.
			/// </summary>
			/// <param name="currentValue">The amount of cards in the player's hand.</param>
			/// <returns>A new constant expression.</returns>
			IHandConstantIntExpression HandConstant(int currentValue);
			
			/// <summary>
			/// Casts the expression to <see cref="IXConstantIntExpression"/>, if it is one.
			/// </summary>
			/// <param name="expression">The expression.</param>
			/// <returns>The <see cref="IXConstantIntExpression"/>, if the given expression is one, or <c>null</c> otherwise.</returns>
			IXConstantIntExpression? AsXConstant(IExpression expression);
			
			/// <summary>
			/// Creates a constant expression with the given value, based on the current value of <see cref="AVariableHint">the X variable</see>.
			/// </summary>
			/// <param name="currentValue">The value of the X variable.</param>
			/// <returns>A new constant expression.</returns>
			IXConstantIntExpression XConstant(int currentValue);
			
			/// <summary>
			/// Casts the expression to <see cref="IMultiplyIntExpression"/>, if it is one.
			/// </summary>
			/// <param name="expression">The expression.</param>
			/// <returns>The <see cref="IMultiplyIntExpression"/>, if the given expression is one, or <c>null</c> otherwise.</returns>
			IMultiplyIntExpression? AsMultiply(IExpression expression);
			
			/// <summary>
			/// Creates an expression which multiplies another expression by a number.
			/// </summary>
			/// <param name="expression">The expression to multiply.</param>
			/// <param name="scalar">The value to multiply the expression by.</param>
			/// <returns>A new expression.</returns>
			IMultiplyIntExpression Multiply(IIntExpression expression, int scalar);
			
			/// <summary>
			/// Casts the expression to <see cref="IHasStatusExpression"/>, if it is one.
			/// </summary>
			/// <param name="expression">The expression.</param>
			/// <returns>The <see cref="IHasStatusExpression"/>, if the given expression is one, or <c>null</c> otherwise.</returns>
			IHasStatusExpression? AsHasStatus(IExpression expression);
			
			/// <summary>
			/// Creates an expression that is true if the player/enemy ship has the given status.
			/// </summary>
			/// <param name="status">The status to check for.</param>
			/// <param name="targetPlayer">Whether the expression looks for the status on the player's ship (<c>true</c>) or the enemy's ship (<c>false</c>).</param>
			/// <returns>A new expression.</returns>
			IHasStatusExpression HasStatus(Status status, bool targetPlayer = true);
			
			/// <summary>
			/// Casts the expression to <see cref="IStatusExpression"/>, if it is one.
			/// </summary>
			/// <param name="expression">The expression.</param>
			/// <returns>The <see cref="IStatusExpression"/>, if the given expression is one, or <c>null</c> otherwise.</returns>
			IStatusExpression? AsStatus(IExpression expression);
			
			/// <summary>
			/// Creates an expression reflecting the current amount of the given status the player/enemy ship has.
			/// </summary>
			/// <param name="status">The status to check for.</param>
			/// <param name="targetPlayer">Whether the expression looks for the status on the player's ship (<c>true</c>) or the enemy's ship (<c>false</c>).</param>
			/// <returns>A new expression.</returns>
			IStatusExpression Status(Status status, bool targetPlayer = true);
			
			/// <summary>
			/// Casts the expression to <see cref="IEquation"/>, if it is one.
			/// </summary>
			/// <param name="expression">The expression.</param>
			/// <returns>The <see cref="IEquation"/>, if the given expression is one, or <c>null</c> otherwise.</returns>
			IEquation? AsEquation(IExpression expression);
			
			/// <summary>
			/// Creates an equation expression that compares two values to each other.
			/// </summary>
			/// <param name="lhs">The left-hand side of the equation.</param>
			/// <param name="operator">The operator to compare the two sides with.</param>
			/// <param name="rhs">The right-hand side of the equation.</param>
			/// <param name="style">The wording style to use for the tooltip of the equation.</param>
			/// <returns>A new equation expression.</returns>
			IEquation Equation(IIntExpression lhs, EquationOperator @operator, IIntExpression rhs, EquationStyle style);

			/// <summary>
			/// Casts the action to <see cref="IConditionalAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IConditionalAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IConditionalAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new conditional action.
			/// </summary>
			/// <param name="expression">The condition expression.</param>
			/// <param name="action">The action to run.</param>
			/// <returns>A new conditional action.</returns>
			IConditionalAction MakeAction(IBoolExpression expression, CardAction action);

			/// <summary>
			/// The operator to compare two sides of an <see cref="IEquation">equation</see> with.
			/// </summary>
			[JsonConverter(typeof(StringEnumConverter))]
			public enum EquationOperator
			{
				/// <summary>
				/// Whether the two sides are equal.
				/// </summary>
				Equal,
				
				/// <summary>
				/// Whether the two sides are not equal.
				/// </summary>
				NotEqual,
				
				/// <summary>
				/// Whether the left-hand side is greater than the right-hand side.
				/// </summary>
				GreaterThan,
				
				/// <summary>
				/// Whether the left-hand side is less than the right-hand side.
				/// </summary>
				LessThan,
				
				/// <summary>
				/// Whether the left-hand side is greater than or equal to the right-hand side.
				/// </summary>
				GreaterThanOrEqual,
				
				/// <summary>
				/// Whether the left-hand side is less than or equal to the right-hand side.
				/// </summary>
				LessThanOrEqual
			}

			/// <summary>
			/// The wording style to use for the tooltip of an equation.
			/// </summary>
			[JsonConverter(typeof(StringEnumConverter))]
			public enum EquationStyle
			{
				/// <summary>
				/// <list type="bullet">
				/// <item>"(...) <c>{Left}</c> is <c>{Right}</c>."</item>
				/// <item>"(...) <c>{Left}</c> is not <c>{Right}</c>."</item>
				/// <item>"(...) <c>{Left}</c> is greater than <c>{Right}</c>."</item>
				/// <item>"(...) <c>{Left}</c> is less than <c>{Right}</c>."</item>
				/// <item>"(...) <c>{Left}</c> is greater than or equal to <c>{Right}</c>."</item>
				/// <item>"(...) <c>{Left}</c> is less than or equal to <c>{Right}</c>."</item>
				/// </list>
				/// </summary>
				Formal,
				
				/// <summary>
				/// <list type="bullet">
				/// <item>"(...) you are at <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you are not at <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you are higher than <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you are lower than <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you are at least at <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you are at most at <c>{Right}</c> <c>{Left}</c>."</item>
				/// </list>
				/// </summary>
				State,
				
				/// <summary>
				/// <list type="bullet">
				/// <item>"(...) you have exactly <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you do not have exactly <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you have more than <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you have less than <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you have at least <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) you have at most <c>{Right}</c> <c>{Left}</c>."</item>
				/// </list>
				/// </summary>
				Possession,
				
				/// <summary>
				/// <list type="bullet">
				/// <item>"(...) you have the same amount of <c>{Left}</c> and <c>{Right}</c>."</item>
				/// <item>"(...) you have a different amount of <c>{Left}</c> and <c>{Right}</c>."</item>
				/// <item>"(...) you have more <c>{Left}</c> than <c>{Right}</c>."</item>
				/// <item>"(...) you have less <c>{Left}</c> than <c>{Right}</c>."</item>
				/// <item>"(...) you have at least as much <c>{Left}</c> as <c>{Right}</c>."</item>
				/// <item>"(...) you have at most as much <c>{Left}</c> as <c>{Right}</c>."</item>
				/// </list>
				/// </summary>
				PossessionComparison,

				/// <summary>
				/// <list type="bullet">
				/// <item>"(...) your enemy is at <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy is not at <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy is higher than <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy is lower than <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy is at least at <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy is at most at <c>{Right}</c> <c>{Left}</c>."</item>
				/// </list>
				/// </summary>
				EnemyState,
				
				/// <summary>
				/// <list type="bullet">
				/// <item>"(...) your enemy has exactly <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy does not have exactly <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy has more than <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy has less than <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy has at least <c>{Right}</c> <c>{Left}</c>."</item>
				/// <item>"(...) your enemy has at most <c>{Right}</c> <c>{Left}</c>."</item>
				/// </list>
				/// </summary>
				EnemyPossession,
				
				/// <summary>
				/// <list type="bullet">
				/// <item>"(...) your enemy has the same amount of <c>{Left}</c> and <c>{Right}</c>."</item>
				/// <item>"(...) your enemy has a different amount of <c>{Left}</c> and <c>{Right}</c>."</item>
				/// <item>"(...) your enemy has more <c>{Left}</c> than <c>{Right}</c>."</item>
				/// <item>"(...) your enemy has less <c>{Left}</c> than <c>{Right}</c>."</item>
				/// <item>"(...) your enemy has at least as much <c>{Left}</c> as <c>{Right}</c>."</item>
				/// <item>"(...) your enemy has at most as much <c>{Left}</c> as <c>{Right}</c>."</item>
				/// </list>
				/// </summary>
				EnemyPossessionComparison,

				/// <summary>
				/// <list type="bullet">
				/// <item>"(...) you have the same amount of <c>{Left}</c> as your enemy."</item>
				/// <item>"(...) you have a different amount of <c>{Left}</c> than your enemy."</item>
				/// <item>"(...) you have more <c>{Left}</c> than your enemy."</item>
				/// <item>"(...) you have less <c>{Left}</c> than your enemy."</item>
				/// <item>"(...) you have at least as much <c>{Left}</c> as your enemy."</item>
				/// <item>"(...) you have at most as much <c>{Left}</c> as your enemy."</item>
				/// </list>
				/// </summary>
				ToEnemyPossessionComparison
			}

			/// <summary>
			/// Represents a conditional expression with a value of any type.
			/// </summary>
			public interface IExpression
			{
				/// <summary>
				/// Renders the conditional expression.
				/// </summary>
				/// <param name="g">The global game state.</param>
				/// <param name="position">The modifiable position to render at.</param>
				/// <param name="isDisabled">Whether the action is disabled.</param>
				/// <param name="dontRender"><c>true</c> when the method is only called to retrieve the width of the action, <c>false</c> if it should actually be rendered.</param>
				void Render(G g, ref Vec position, bool isDisabled, bool dontRender);
				
				/// <summary>
				/// Returns a description for the expression that will be used as part of a conditional action's tooltip.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <returns></returns>
				string GetTooltipDescription(State state, Combat combat);
				
				/// <summary>
				/// Provides a list of tooltips for this conditional expression.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <returns>The list of tooltips for this conditional expression.</returns>
				IReadOnlyList<Tooltip> GetTooltips(State state, Combat combat) => [];
			}

			/// <summary>
			/// Represents a conditional expression with a <see cref="bool"/> value.
			/// </summary>
			public interface IBoolExpression : IExpression
			{
				/// <summary>
				/// Returns the current value of the expression.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <returns>The value.</returns>
				bool GetValue(State state, Combat combat);
				
				/// <summary>
				/// Provides a list of tooltips which should override the default conditional expression tooltip.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <param name="defaultTooltip">The default conditional expression tooltip.</param>
				/// <param name="defaultTooltipDescription">The description part of the default conditional expression tooltip.</param>
				/// <returns>The list of tooltips which should override the default conditional expression tooltip.</returns>
				IEnumerable<Tooltip> OverrideConditionalTooltip(State state, Combat combat, Tooltip defaultTooltip, string defaultTooltipDescription) => [defaultTooltip];
			}
			
			/// <summary>
			/// Represents a conditional expression with an <see cref="int"/> value.
			/// </summary>
			public interface IIntExpression : IExpression
			{
				/// <summary>
				/// Returns the current value of the expression.
				/// </summary>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <returns>The value.</returns>
				int GetValue(State state, Combat combat);
			}

			/// <summary>
			/// Represents a constant expression with the given value.
			/// </summary>
			public interface IConstantIntExpression : IIntExpression
			{
				/// <summary>
				/// The constant value.
				/// </summary>
				int Value { get; set; }

				/// <summary>
				/// Sets <see cref="Value"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IConstantIntExpression SetValue(int value);
			}

			/// <summary>
			/// Represents a constant expression with the given value, based on the current amount of cards in the player's hand.
			/// </summary>
			public interface IHandConstantIntExpression : IIntExpression
			{
				/// <summary>
				/// The current amount of cards in the player's hand.
				/// </summary>
				int CurrentValue { get; set; }

				/// <summary>
				/// Sets <see cref="CurrentValue"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IHandConstantIntExpression SetCurrentValue(int value);
			}

			/// <summary>
			/// Represents a constant expression with the given value, based on the current value of <see cref="AVariableHint">the X variable</see>.
			/// </summary>
			public interface IXConstantIntExpression : IIntExpression
			{
				/// <summary>
				/// The current value of <see cref="AVariableHint">the X variable</see>.
				/// </summary>
				int CurrentValue { get; set; }

				/// <summary>
				/// Sets <see cref="CurrentValue"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IXConstantIntExpression SetCurrentValue(int value);
			}

			/// <summary>
			/// Represents an expression which multiplies another expression by a number.
			/// </summary>
			public interface IMultiplyIntExpression : IIntExpression
			{
				/// <summary>
				/// The expression to multiply.
				/// </summary>
				IIntExpression Expression { get; set; }
				
				/// <summary>
				/// The value to multiply the expression by.
				/// </summary>
				int Scalar { get; set; }

				/// <summary>
				/// Sets <see cref="Expression"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IMultiplyIntExpression SetExpression(IIntExpression value);
				
				/// <summary>
				/// Sets <see cref="Scalar"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IMultiplyIntExpression SetScalar(int value);
			}

			/// <summary>
			/// Represents an expression that is true if the player/enemy ship has the given status.
			/// </summary>
			public interface IHasStatusExpression : IBoolExpression
			{
				/// <summary>
				/// The status to check for.
				/// </summary>
				Status Status { get; set; }
				
				/// <summary>
				/// Whether the expression looks for the status on the player's ship (<c>true</c>) or the enemy's ship (<c>false</c>).
				/// </summary>
				bool TargetPlayer { get; set; }
				
				/// <summary>
				/// Whether having a negative amount of a status counts as having it.
				/// </summary>
				bool CountNegative { get; set; }

				/// <summary>
				/// Sets <see cref="Status"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IHasStatusExpression SetStatus(Status value);
				
				/// <summary>
				/// Sets <see cref="TargetPlayer"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IHasStatusExpression SetTargetPlayer(bool value);
				
				/// <summary>
				/// Sets <see cref="CountNegative"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IHasStatusExpression SetCountNegative(bool value);
			}

			/// <summary>
			/// Represents an expression reflecting the current amount of the given status the player/enemy ship has.
			/// </summary>
			public interface IStatusExpression : IIntExpression
			{
				/// <summary>
				/// The status to check for.
				/// </summary>
				Status Status { get; set; }
				
				/// <summary>
				/// Whether the expression looks for the status on the player's ship (<c>true</c>) or the enemy's ship (<c>false</c>).
				/// </summary>
				bool TargetPlayer { get; set; }

				/// <summary>
				/// Sets <see cref="Status"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IStatusExpression SetStatus(Status value);
				
				/// <summary>
				/// Sets <see cref="TargetPlayer"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IStatusExpression SetTargetPlayer(bool value);
			}

			/// <summary>
			/// Represents an equation expression that compares two values to each other.
			/// </summary>
			public interface IEquation : IBoolExpression
			{
				/// <summary>
				/// The left-hand side of the equation.
				/// </summary>
				IIntExpression Lhs { get; set; }
				
				/// <summary>
				/// The operator to compare the two sides with.
				/// </summary>
				EquationOperator Operator { get; set; }
				
				/// <summary>
				/// The right-hand side of the equation.
				/// </summary>
				IIntExpression Rhs { get; set; }
				
				/// <summary>
				/// The wording style to use for the tooltip of the equation.
				/// </summary>
				EquationStyle Style { get; set; }
				
				/// <summary>
				/// Whether the operator should be shown (<c>true</c>) or it is implied (<c>false</c>).
				/// </summary>
				bool ShowOperator { get; set; }

				/// <summary>
				/// Sets <see cref="Lhs"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IEquation SetLhs(IIntExpression value);
				
				/// <summary>
				/// Sets <see cref="Operator"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IEquation SetOperator(EquationOperator value);
				
				/// <summary>
				/// Sets <see cref="Rhs"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IEquation SetRhs(IIntExpression value);
				
				/// <summary>
				/// Sets <see cref="Style"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IEquation SetStyle(EquationStyle value);
				
				/// <summary>
				/// Sets <see cref="ShowOperator"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IEquation SetShowOperator(bool value);
			}

			/// <summary>
			/// Represents a conditional action.
			/// </summary>
			public interface IConditionalAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The conditional expression.
				/// </summary>
				IBoolExpression Expression { get; set; }
				
				/// <summary>
				/// The action to run.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// Whether the whole action should be rendered like a disabled action if the condition is not satisfied.
				/// </summary>
				bool FadeUnsatisfied { get; set; }
				
				/// <summary>
				/// Whether a question mark should be rendered at the end of the condition.
				/// </summary>
				bool ShowQuestionMark { get; set; }
				
				/// <summary>
				/// Sets <see cref="Expression"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IConditionalAction SetExpression(IBoolExpression value);
				
				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IConditionalAction SetAction(CardAction value);
				
				/// <summary>
				/// Sets <see cref="FadeUnsatisfied"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IConditionalAction SetFadeUnsatisfied(bool value);
				
				/// <summary>
				/// Sets <see cref="ShowQuestionMark"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IConditionalAction SetShowQuestionMark(bool value);
			}
		}
	}
}
