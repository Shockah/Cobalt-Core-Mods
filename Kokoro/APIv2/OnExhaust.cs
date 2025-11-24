namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IOnExhaustApi"/>
		IOnExhaustApi OnExhaust { get; }

		/// <summary>
		/// Allows using <see cref="CardAction">card actions</see> which only trigger when the card is exhausted.
		/// </summary>
		public interface IOnExhaustApi
		{
			/// <summary>
			/// Casts the action as an on exhaust action, if it is one.
			/// </summary>
			/// <param name="action">The potential on exhaust action.</param>
			/// <returns>The on exhaust action, if the given action is one, or <c>null</c> otherwise.</returns>
			IOnExhaustAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new on exhaust action, wrapping the provided action.
			/// </summary>
			/// <param name="action">The action to wrap.</param>
			/// <returns>The new on exhaust action.</returns>
			IOnExhaustAction MakeAction(CardAction action);
			
			/// <summary>
			/// Represents an action, which only triggers when the card is exhausted.
			/// </summary>
			public interface IOnExhaustAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The actual action to run on exhaust.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// Whether to show the icon for the wrapper action.
				/// </summary>
				bool ShowOnExhaustIcon { get; set; }
				
				/// <summary>
				/// Whether to show the tooltip for the wrapper action.
				/// </summary>
				bool ShowOnExhaustTooltip { get; set; }

				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IOnExhaustAction SetAction(CardAction value);

				/// <summary>
				/// Sets <see cref="ShowOnExhaustIcon"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IOnExhaustAction SetShowOnExhaustIcon(bool value);

				/// <summary>
				/// Sets <see cref="ShowOnExhaustTooltip"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IOnExhaustAction SetShowOnExhaustTooltip(bool value);
			}
		}
	}
}
