namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IOnDiscardApi"/>
		IOnDiscardApi OnDiscard { get; }

		/// <summary>
		/// Allows using <see cref="CardAction">card actions</see> which only trigger when the card is discarded (but not by playing the card).
		/// </summary>
		public interface IOnDiscardApi
		{
			/// <summary>
			/// Casts the action as an on discard action, if it is one.
			/// </summary>
			/// <param name="action">The potential on discard action.</param>
			/// <returns>The on discard action, if the given action is one, or <c>null</c> otherwise.</returns>
			IOnDiscardAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new on discard action, wrapping the provided action.
			/// </summary>
			/// <param name="action">The action to wrap.</param>
			/// <returns>The new on discard action.</returns>
			IOnDiscardAction MakeAction(CardAction action);
			
			/// <summary>
			/// Represents an action, which only triggers when the card is discarded (but not by playing the card).
			/// </summary>
			public interface IOnDiscardAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The actual action to run on discard.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// Whether to show the icon for the wrapper action.
				/// </summary>
				bool ShowOnDiscardIcon { get; set; }
				
				/// <summary>
				/// Whether to show the tooltip for the wrapper action.
				/// </summary>
				bool ShowOnDiscardTooltip { get; set; }

				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IOnDiscardAction SetAction(CardAction value);

				/// <summary>
				/// Sets <see cref="ShowOnDiscardIcon"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IOnDiscardAction SetShowOnDiscardIcon(bool value);

				/// <summary>
				/// Sets <see cref="ShowOnDiscardTooltip"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IOnDiscardAction SetShowOnDiscardTooltip(bool value);
			}
		}
	}
}
