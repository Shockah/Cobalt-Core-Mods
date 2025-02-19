using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ISequenceApi"/>
		ISequenceApi Sequence { get; }

		/// <summary>
		/// Allows access to actions, which only trigger every N plays of the card each combat.
		/// </summary>
		public interface ISequenceApi
		{
			/// <summary>
			/// Casts the action as a sequence action, if it is one.
			/// </summary>
			/// <param name="action">The potential sequence action.</param>
			/// <returns>The sequence action, if the given action is one, or <c>null</c> otherwise.</returns>
			ISequenceAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new sequence action, wrapping the provided action.
			/// </summary>
			/// <param name="cardId">The ID of the card to check the amount of times it was played this combat.</param>
			/// <param name="interval">The interval in which the sequence resets.</param>
			/// <param name="sequenceStep">The step of the sequence at which this action triggers.</param>
			/// <param name="sequenceLength">The total length of the sequence.</param>
			/// <param name="action">The action to wrap.</param>
			/// <returns>The new sequence action.</returns>
			ISequenceAction MakeAction(int cardId, Interval interval, int sequenceStep, int sequenceLength, CardAction action);

			/// <summary>
			/// Defines an interval in which the sequence resets.
			/// </summary>
			[JsonConverter(typeof(StringEnumConverter))]
			public enum Interval
			{
				/// <summary>
				/// The sequence is never reset during a run.
				/// </summary>
				Run,
				
				/// <summary>
				/// The sequence resets each combat.
				/// </summary>
				Combat,
				
				/// <summary>
				/// The sequence resets each turn.
				/// </summary>
				Turn
			}
			
			/// <summary>
			/// Represents an action, which only triggers every N plays of the card each combat.
			/// </summary>
			/// <para>
			/// A sequence 1/3 action (step 1, length 3) will trigger its action on the first play of the card each combat, and then will not trigger for the following two plays.
			/// The fourth play of the card will again trigger the action.
			/// </para>
			/// <para>
			/// A sequence 2/3 action (step 2, length 3) will trigger its action on the second play of the card, then the fifth play, etc.
			/// </para>
			/// <para>
			/// A single card may contain multiple sequences of different lengths, and they will trigger and cycle independently.
			/// </para>
			public interface ISequenceAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The ID of the card to check the amount of times it was played this combat.
				/// </summary>
				int CardId { get; set; }
				
				/// <summary>
				/// The interval in which the sequence resets.
				/// </summary>
				Interval Interval { get; set; }
				
				/// <summary>
				/// The step of the sequence at which this action triggers.
				/// </summary>
				int SequenceStep { get; set; }
				
				/// <summary>
				/// The total length of the sequence.
				/// </summary>
				int SequenceLength { get; set; }
				
				/// <summary>
				/// The actual action to run every N-th play of the card.
				/// </summary>
				CardAction Action { get; set; }

				/// <summary>
				/// Sets <see cref="CardId"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISequenceAction SetCardId(int value);

				/// <summary>
				/// Sets <see cref="Interval"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISequenceAction SetInterval(Interval value);
				
				/// <summary>
				/// Sets <see cref="SequenceStep"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISequenceAction SetSequenceStep(int value);
				
				/// <summary>
				/// Sets <see cref="SequenceLength"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISequenceAction SetSequenceLength(int value);
				
				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISequenceAction SetAction(CardAction value);
			}
		}
	}
}
