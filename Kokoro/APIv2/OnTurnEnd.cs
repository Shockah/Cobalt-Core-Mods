namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IOnTurnEndApi"/>
		IOnTurnEndApi OnTurnEnd { get; }

		/// <summary>
		/// Allows using <see cref="CardAction">card actions</see> which only trigger on turn end, if the card is still in hand.
		/// </summary>
		public interface IOnTurnEndApi
		{
			/// <summary>
			/// Casts the action as an on turn end action, if it is one.
			/// </summary>
			/// <param name="action">The potential on turn end action.</param>
			/// <returns>The on turn end action, if the given action is one, or <c>null</c> otherwise.</returns>
			IOnTurnEndAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new on turn end action, wrapping the provided action.
			/// </summary>
			/// <param name="action">The action to wrap.</param>
			/// <returns>The new on turn end action.</returns>
			IOnTurnEndAction MakeAction(CardAction action);
			
			/// <summary>
			/// Represents an action, which only triggers on turn end, if the card is still in hand.
			/// </summary>
			public interface IOnTurnEndAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The actual action to run on turn end.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// Whether to show the icon for the wrapper action.
				/// </summary>
				bool ShowOnTurnEndIcon { get; set; }
				
				/// <summary>
				/// Whether to show the tooltip for the wrapper action.
				/// </summary>
				bool ShowOnTurnEndTooltip { get; set; }

				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IOnTurnEndAction SetAction(CardAction value);

				/// <summary>
				/// Sets <see cref="ShowOnTurnEndIcon"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IOnTurnEndAction SetShowOnTurnEndIcon(bool value);

				/// <summary>
				/// Sets <see cref="ShowOnTurnEndTooltip"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IOnTurnEndAction SetShowOnTurnEndTooltip(bool value);
			}
		}
	}
}
