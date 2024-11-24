namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ISpoofedActionsApi"/>
		ISpoofedActionsApi SpoofedActions { get; }

		/// <summary>
		/// Allows using spoofed <see cref="CardAction">card actions</see>.
		/// These actions show up visually as one action, but actually do a different thing when ran.
		/// </summary>
		public interface ISpoofedActionsApi
		{
			/// <summary>
			/// Casts the action as a spoofed action, if it is one.
			/// </summary>
			/// <param name="action">The potential spoofed action.</param>
			/// <returns>The spoofed action, if the given action is one, or <c>null</c> otherwise.</returns>
			ISpoofedAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new spoofed action, wrapping the provided real action, and spoofing it with the provided render action.
			/// </summary>
			/// <param name="renderAction">The action to render.</param>
			/// <param name="realAction">The action to run.</param>
			/// <returns>The new spoofed action.</returns>
			ISpoofedAction MakeAction(CardAction renderAction, CardAction realAction);
			
			/// <summary>
			/// Represents a spoofed action.
			/// These actions show up visually as one action, but actually do a different thing when ran.
			/// </summary>
			public interface ISpoofedAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The action to render.
				/// </summary>
				CardAction RenderAction { get; set; }
				
				/// <summary>
				/// The action to run.
				/// </summary>
				CardAction RealAction { get; set; }

				/// <summary>
				/// Sets <see cref="RenderAction"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISpoofedAction SetRenderAction(CardAction value);
				
				/// <summary>
				/// Sets <see cref="RealAction"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ISpoofedAction SetRealAction(CardAction value);
			}
		}
	}
}
