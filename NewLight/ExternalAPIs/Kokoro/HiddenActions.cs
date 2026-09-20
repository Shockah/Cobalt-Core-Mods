namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IHiddenActionsApi"/>
		IHiddenActionsApi HiddenActions { get; }

		/// <summary>
		/// Allows using hidden <see cref="CardAction">card actions</see>.
		/// These actions do not actually show up visually on cards (and do not take space either), but still run like any other action.
		/// </summary>
		public interface IHiddenActionsApi
		{
			/// <summary>
			/// Casts the action as a hidden action, if it is one.
			/// </summary>
			/// <param name="action">The potential hidden action.</param>
			/// <returns>The hidden action, if the given action is one, or <c>null</c> otherwise.</returns>
			IHiddenAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new hidden action, wrapping and hiding the provided action.
			/// </summary>
			/// <param name="action">The action to hide.</param>
			/// <returns>The new hidden action.</returns>
			IHiddenAction MakeAction(CardAction action);
			
			/// <summary>
			/// Represents a hidden action.
			/// These actions do not actually show up visually on cards (and do not take space either), but still run like any other action.
			/// </summary>
			public interface IHiddenAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The action being hidden.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// Whether tooltips for the hidden action should be shown.
				/// </summary>
				bool ShowTooltips { get; set; }

				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IHiddenAction SetAction(CardAction value);
				
				/// <summary>
				/// Sets <see cref="ShowTooltips"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IHiddenAction SetShowTooltips(bool value);
			}
		}
	}
}
