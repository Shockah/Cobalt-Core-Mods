using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
			/// Creates a new variable hint action for the amount of times the given card was played in a given interval.
			/// </summary>
			/// <param name="cardId">The ID of the card to check the amount of times it was played in a given interval.</param>
			/// <param name="interval">The interval in which the amount of times a card was played is tracked.</param>
			/// <returns>The new variable hint action.</returns>
			ITimesPlayedVariableHintAction MakeVariableHintAction(int cardId, Interval interval);
			
			/// <summary>
			/// Returns the amount of times the given card was played in a given interval.
			/// </summary>
			/// <param name="card">The card to check.</param>
			/// <param name="interval">The interval in which the amount of times a card was played is tracked.</param>
			/// <returns></returns>
			int GetTimesPlayed(Card card, Interval interval);

			/// <summary>
			/// Forcibly sets the amount of times the given card was played in a given interval.
			/// </summary>
			/// <param name="card">The card to set the value for.</param>
			/// <param name="interval">The interval in which the amount of times a card was played is tracked.</param>
			/// <param name="value">The amount.</param>
			void SetTimesPlayed(Card card, Interval interval, int value);

			/// <summary>
			/// Defines an interval in which the amount of times a card was played is tracked.
			/// </summary>
			[JsonConverter(typeof(StringEnumConverter))]
			public enum Interval
			{
				/// <summary>
				/// Amount of times a card was played this run.
				/// </summary>
				Run,
				
				/// <summary>
				/// Amount of times a card was played this combat.
				/// </summary>
				Combat,
				
				/// <summary>
				/// Amount of times a card was played this turn.
				/// </summary>
				Turn
			}
			
			/// <summary>
			/// A variable hint action for the amount of times a card was played this combat.
			/// </summary>
			public interface ITimesPlayedVariableHintAction : ICardAction<AVariableHint>
			{
				/// <summary>
				/// The ID of the card to check the amount of times it was played in a given interval.
				/// </summary>
				int CardId { get; set; }
				
				/// <summary>
				/// The interval in which the amount of times a card was played is tracked.
				/// </summary>
				Interval Interval { get; set; }

				/// <summary>
				/// Sets <see cref="CardId"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITimesPlayedVariableHintAction SetCardId(int value);

				/// <summary>
				/// Sets <see cref="Interval"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ITimesPlayedVariableHintAction SetInterval(Interval value);
			}
		}
	}
}
