namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ITimesPlayedApi"/>
		ITimesPlayedApi TimesPlayed { get; }

		/// <summary>
		/// Allows access to actions, which care about how many times a card was played each combat.
		/// </summary>
		public interface ITimesPlayedApi
		{
			/// <summary>
			/// Casts the action to <see cref="ITimesPlayedVariableHintAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="ITimesPlayedVariableHintAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			ITimesPlayedVariableHintAction? AsVariableHintAction(CardAction action);

			/// <summary>
			/// Creates a new variable hint action for the amount of times the given card was played this combat.
			/// </summary>
			/// <param name="cardId">The ID of the card to check the amount of times it was played this combat.</param>
			/// <returns>The new variable hint action.</returns>
			ITimesPlayedVariableHintAction MakeVariableHintAction(int cardId);
			
			/// <summary>
			/// Casts the condition expression to <see cref="ITimesPlayedConditionExpression"/>, if it is one.
			/// </summary>
			/// <param name="expression">The expression.</param>
			/// <returns>The <see cref="ITimesPlayedConditionExpression"/>, if the given expression is one, or <c>null</c> otherwise.</returns>
			ITimesPlayedConditionExpression? AsConditionExpression(IConditionalApi.IExpression expression);

			/// <summary>
			/// Creates a new condition expression, representing the amount of times the given card was played this combat.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="cardId">The ID of the card to check the amount of times it was played this combat.</param>
			/// <returns>The new expression.</returns>
			ITimesPlayedConditionExpression MakeConditionExpression(State state, Combat combat, int cardId);
			
			/// <summary>
			/// Returns the amount of times the given card was played this combat.
			/// </summary>
			/// <param name="card">The card to check.</param>
			/// <returns></returns>
			int GetTimesPlayed(Card card);

			/// <summary>
			/// Forcibly sets the amount of times the given card was played this combat.
			/// </summary>
			/// <param name="card">The card to set the value for.</param>
			/// <param name="value">The amount.</param>
			void SetTimesPlayed(Card card, int value);
			
			/// <summary>
			/// A variable hint action for the amount of times a card was played this combat.
			/// </summary>
			public interface ITimesPlayedVariableHintAction : ICardAction<AVariableHint>
			{
				/// <summary>
				/// The ID of the card to check the amount of times it was played this combat.
				/// </summary>
				int CardId { get; set; }

				/// <summary>
				/// Sets <see cref="CardId"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITimesPlayedVariableHintAction SetCardId(int value);
			}

			/// <summary>
			/// A numeric expression for the amount of times a card was played this combat.
			/// </summary>
			public interface ITimesPlayedConditionExpression : IConditionalApi.IIntExpression
			{
				/// <summary>
				/// The ID of the card this condition is for.
				/// </summary>
				int CardId { get; }

				/// <summary>
				/// The current amount of times the card was played when this condition was created.
				/// </summary>
				/// <remarks>
				/// This condition has to cache the amount of times the card was played, as this number changes between rendering it on a card and actually playing it.
				/// </remarks>
				int CurrentTimesPlayed { get; set; }
				
				/// <summary>
				/// Sets the ID of the card to check the amount of times it was played this combat.
				/// </summary>
				/// <remarks>
				/// This method additionally updates the <see cref="CurrentTimesPlayed"/>.
				/// </remarks>
				/// <param name="state">The game state.</param>
				/// <param name="combat">The current combat.</param>
				/// <param name="cardId">The new ID of the card this condition is for.</param>
				/// <returns>This object after the change.</returns>
				ITimesPlayedConditionExpression SetCard(State state, Combat combat, int cardId);
				
				/// <summary>
				/// Sets the current amount of times the card was played when this condition was created.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITimesPlayedConditionExpression SetCurrentTimesPlayed(int value);
			}
		}
	}
}
