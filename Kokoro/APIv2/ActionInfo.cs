using System;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IActionInfoApi"/>
		IActionInfoApi ActionInfo { get; }

		/// <summary>
		/// Allows checking additional action information, like what card an action came from.
		/// </summary>
		public interface IActionInfoApi
		{
			/// <summary>
			/// Returns the ID of the card the given action came from.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The ID of the card the given action came from.</returns>
			int? GetSourceCardId(CardAction action);

			/// <summary>
			/// Returns the card the given action came from.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="action">The action.</param>
			/// <returns>The card the given action came from.</returns>
			Card? GetSourceCard(State state, CardAction action);

			/// <summary>
			/// Sets the ID of the card the given action came from.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <param name="sourceId">The ID of the card the given action came from.</param>
			void SetSourceCardId(CardAction action, int? sourceId);

			/// <summary>
			/// Sets the card the given action came from.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <param name="state">The game state.</param>
			/// <param name="source">The card the given action came from.</param>
			void SetSourceCard(CardAction action, State state, Card? source);

			/// <summary>
			/// Returns the ID of the interaction the given action came from.
			/// All actions of a single <see cref="Card.GetActionsOverridden"/> call will have the same ID.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The ID of the interaction.</returns>
			Guid? GetInteractionId(CardAction action);
			
			/// <summary>
			/// Sets the ID of the interaction the given action came from.
			/// All actions of a single <see cref="Card.GetActionsOverridden"/> call should have the same ID.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <param name="interactionId">The ID of the interaction.</param>
			void SetInteractionId(CardAction action, Guid? interactionId);
			
			/// <summary>
			/// Registers a new hook related to action costs.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to action costs.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// A hook related to action information.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Called when a user interaction has ended (for example, all actions of a card were executed).
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void OnInteractionEnded(IOnInteractionEndedArgs args) { }
				
				/// <summary>
				/// The arguments for the <see cref="OnInteractionEnded"/> hook method.
				/// </summary>
				public interface IOnInteractionEndedArgs
				{
					/// <summary>
					/// The game state.
					/// </summary>
					State State { get; }
					
					/// <summary>
					/// The current combat.
					/// </summary>
					Combat Combat { get; }
				
					/// <summary>
					/// The card this interaction was for, if any.
					/// </summary>
					Card? Card { get; }
					
					/// <summary>
					/// The ID of the interaction.
					/// All actions of a single <see cref="Card.GetActionsOverridden"/> call will have the same ID.
					/// </summary>
					Guid InteractionId { get; }
				}
			}
		}
	}
}
