namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IRedrawStatusApi"/>
		IRedrawStatusApi RedrawStatus { get; }

		/// <summary>
		/// Allows access to the Redraw status.
		/// Whenever the player has any Redraw, they can freely choose any card to discard and draw a new one instead, at the cost of 1 Redraw.
		/// </summary>
		public interface IRedrawStatusApi
		{
			/// <summary>
			/// The status.
			/// </summary>
			Status Status { get; }
			
			/// <summary>
			/// A Redraw hook that handles the default payment option (paying with 1 Redraw).
			/// </summary>
			IHook StandardRedrawStatusPaymentHook { get; }
			
			/// <summary>
			/// A Redraw hook that handles the actual default action (discarding a card and drawing a new one).
			/// </summary>
			IHook StandardRedrawStatusActionHook { get; }
			
			/// <summary>
			/// Tests whether it is currently possible to redraw the given card.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="card">The card.</param>
			/// <returns>Whether it is currently possible to redraw the given card.</returns>
			bool IsRedrawPossible(State state, Combat combat, Card card);
			
			/// <summary>
			/// Attempt to redraw the given card.
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="combat">The current combat.</param>
			/// <param name="card">The card.</param>
			/// <returns>Whether the action succeeded.</returns>
			bool DoRedraw(State state, Combat combat, Card card);
			
			/// <summary>
			/// Registers a new hook related to the Redraw status.
			/// </summary>
			/// <param name="hook">The hook.</param>
			/// <param name="priority">The priority for the hook. Higher priority hooks are called before lower priority ones. Defaults to <c>0</c>.</param>
			void RegisterHook(IHook hook, double priority = 0);
			
			/// <summary>
			/// Unregisters the given hook related to the Redraw status.
			/// </summary>
			/// <param name="hook">The hook.</param>
			void UnregisterHook(IHook hook);
			
			/// <summary>
			/// A hook related to the Redraw status.
			/// </summary>
			public interface IHook : IKokoroV2ApiHook
			{
				/// <summary>
				/// Controls whether it is currently possible to redraw the given card.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the card can be redrawn, <c>false</c> if it cannot, <c>null</c> if this hook cannot curently handle this card. Defaults to <c>null</c>.</returns>
				bool? CanRedraw(ICanRedrawArgs args) => null;
				
				/// <summary>
				/// Attempts to pay for redrawing a card.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether payment was handled (and no further hooks should be called). Defaults to <c>false</c>.</returns>
				bool PayForRedraw(IPayForRedrawArgs args) => false;
				
				/// <summary>
				/// Attempts to actually redraw a card.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns>Whether the action was handled (and no further hooks should be called). Defaults to <c>false</c>.</returns>
				bool DoRedraw(IDoRedrawArgs args) => false;
				
				/// <summary>
				/// An event called whenever any card is redrawn.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				void AfterRedraw(IAfterRedrawArgs args) { }
				
				/// <summary>
				/// Controls whether the given hook is taken into consideration.
				/// </summary>
				/// <param name="args">The arguments for the hook method.</param>
				/// <returns><c>true</c> if the hook should be enabled, <c>false</c> if it should not, <c>null</c> if this hook does not care. Defaults to <c>null</c>.</returns>
				bool? OverrideHookEnablement(IOverrideHookEnablementArgs args) => null;

				/// <summary>
				/// Represents a type of hook.
				/// </summary>
				public enum HookType
				{
					/// <summary>
					/// Corresponds to the <see cref="CanRedraw"/> method.
					/// </summary>
					Possibility,
					
					/// <summary>
					/// Corresponds to the <see cref="PayForRedraw"/> method.
					/// </summary>
					Payment,
					
					/// <summary>
					/// Corresponds to the <see cref="DoRedraw"/> method.
					/// </summary>
					Action
				}
				
				/// <summary>
				/// The arguments for the <see cref="CanRedraw"/> hook method.
				/// </summary>
				public interface ICanRedrawArgs
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
					/// The card to redraw.
					/// </summary>
					Card Card { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="PayForRedraw"/> hook method.
				/// </summary>
				public interface IPayForRedrawArgs
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
					/// The card to redraw.
					/// </summary>
					Card Card { get; }
					
					/// <summary>
					/// The hook that decided that it is possible to redraw the card.
					/// </summary>
					IHook PossibilityHook { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="DoRedraw"/> hook method.
				/// </summary>
				public interface IDoRedrawArgs
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
					/// The card to redraw.
					/// </summary>
					Card Card { get; }
					
					/// <summary>
					/// The hook that decided that it is possible to redraw the card.
					/// </summary>
					IHook PossibilityHook { get; }
					
					/// <summary>
					/// The hook that handled the payment of the redraw action.
					/// </summary>
					IHook PaymentHook { get; }
				}
				
				/// <summary>
				/// The arguments for the <see cref="AfterRedraw"/> hook method.
				/// </summary>
				public interface IAfterRedrawArgs
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
					/// The redrawn card.
					/// </summary>
					Card Card { get; }
					
					/// <summary>
					/// The hook that decided that it is possible to redraw the card.
					/// </summary>
					IHook PossibilityHook { get; }
					
					/// <summary>
					/// The hook that handled the payment of the redraw action.
					/// </summary>
					IHook PaymentHook { get; }
					
					/// <summary>
					/// The hook that actually did the redraw action.
					/// </summary>
					IHook ActionHook { get; }
				}

				/// <summary>
				/// The arguments for the <see cref="OverrideHookEnablement"/> hook method.
				/// </summary>
				public interface IOverrideHookEnablementArgs
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
					/// The type of the hook.
					/// </summary>
					HookType HookType { get; }
					
					/// <summary>
					/// The hook.
					/// </summary>
					IHook Hook { get; }
				}
			}
		}
	}
}
