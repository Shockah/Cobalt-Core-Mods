namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ITempHullApi"/>
		ITempHullApi TempHull { get; }

		/// <summary>
		/// Allows creating actions which temporarily remove or grant hull.
		/// </summary>
		public interface ITempHullApi
		{
			/// <summary>
			/// Casts the action as a temp hull loss action, if it is one.
			/// </summary>
			/// <param name="action">The potential temp hull loss action.</param>
			/// <returns>The temp hull loss action, if the given action is one, or <c>null</c> otherwise.</returns>
			ILoseAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new on discard action, wrapping the provided action.
			/// </summary>
			/// <param name="amount">The amount of hull to lose.</param>
			/// <param name="targetPlayer">Whether this action targets the player (<c>true</c>) or the enemy (<c>false</c>).</param>
			/// <returns>The new on discard action.</returns>
			ILoseAction MakeLoseAction(int amount, bool targetPlayer = true);
			
			/// <summary>
			/// Represents an action, which only triggers on turn end, if the card is still in hand.
			/// </summary>
			public interface ILoseAction : ICardAction<CardAction>
			{
				/// <summary>
				/// Whether this action targets the player (<c>true</c>) or the enemy (<c>false</c>).
				/// </summary>
				bool TargetPlayer { get; set; }
				
				/// <summary>
				/// The amount of hull to lose.
				/// </summary>
				int Amount { get; set; }
				
				/// <summary>
				/// Whether the action is prohibited from killing the target ship.
				/// </summary>
				bool CannotKill { get; set; }

				/// <summary>
				/// Sets <see cref="TargetPlayer"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ILoseAction SetTargetPlayer(bool value);

				/// <summary>
				/// Sets <see cref="Amount"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ILoseAction SetAmount(int value);

				/// <summary>
				/// Sets <see cref="CannotKill"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ILoseAction SetCannotKill(bool value);
			}
		}
	}
}