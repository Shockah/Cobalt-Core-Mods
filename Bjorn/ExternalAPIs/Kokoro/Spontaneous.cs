using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ISpontaneousApi"/>
		ISpontaneousApi Spontaneous { get; }

		/// <summary>
		/// Allows using spontaneous <see cref="CardAction">card actions</see>.
		/// These actions only trigger when the card is drawn, or when a turn starts with it still in hand, and only once per turn.
		/// </summary>
		public interface ISpontaneousApi
		{
			/// <summary>
			/// The card trait that is being temporarily added to cards, which already triggered their spontaneous actions this turn.
			/// </summary>
			/// <remarks>
			/// This trait is not meant to be added via <see cref="IModCards"/> methods - it is purely visual, and adding it will not change any behavior.
			/// </remarks>
			ICardTraitEntry SpontaneousTriggeredTrait { get; }
			
			/// <summary>
			/// Casts the action as a spontaneous action, if it is one.
			/// </summary>
			/// <param name="action">The potential spontaneous action.</param>
			/// <returns>The spontaneous action, if the given action is one, or <c>null</c> otherwise.</returns>
			ISpontaneousAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new spontaneous action, wrapping the provided action.
			/// </summary>
			/// <param name="action">The action to wrap.</param>
			/// <returns>The new spontaneous action.</returns>
			ISpontaneousAction MakeAction(CardAction action);
			
			/// <summary>
			/// Represents a spontaneous action.
			/// These actions only trigger when the card is drawn, or when a turn starts with it still in hand, and only once per turn.
			/// </summary>
			public interface ISpontaneousAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The actual action to run.
				/// </summary>
				CardAction Action { get; set; }

				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISpontaneousAction SetAction(CardAction value);
			}
		}
	}
}