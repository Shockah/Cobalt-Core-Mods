namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ITempHullApi"/>
		ITempHullApi TempHull { get; }

		/// <summary>
		/// Allows using actions and statuses which temporarily remove or grant hull.
		/// </summary>
		public interface ITempHullApi
		{
			/// <summary>
			/// When you gain any, gain that much hull. When you lose any hull, lose that much TEMP HULL. Lose all of it and that much hull at turn start.
			/// </summary>
			Status LoseHullLaterStatus { get; }
			
			/// <summary>
			/// Regain {0} hull at the end of combat, or at the end of a turn where you didn't suffer temp hull los. Does not count as healing.
			/// </summary>
			Status RegainHullLaterStatus { get; }
			
			/// <summary>
			/// Casts the action as a temp hull gain action, if it is one.
			/// </summary>
			/// <param name="action">The potential temp hull gain action.</param>
			/// <returns>The temp hull gain action, if the given action is one, or <c>null</c> otherwise.</returns>
			IGainAction? AsGainAction(CardAction action);
			
			/// <summary>
			/// Creates a new temp hull gain action.
			/// </summary>
			/// <param name="amount">The amount of hull to gain.</param>
			/// <param name="targetPlayer">Whether this action targets the player (<c>true</c>) or the enemy (<c>false</c>).</param>
			/// <returns>The new temp hull gain action.</returns>
			IGainAction MakeGainAction(int amount, bool targetPlayer = true);
			
			/// <summary>
			/// Casts the action as a temp hull loss action, if it is one.
			/// </summary>
			/// <param name="action">The potential temp hull loss action.</param>
			/// <returns>The temp hull loss action, if the given action is one, or <c>null</c> otherwise.</returns>
			ILossAction? AsLossAction(CardAction action);
			
			/// <summary>
			/// Creates a new temp hull loss action.
			/// </summary>
			/// <param name="amount">The amount of hull to lose.</param>
			/// <param name="targetPlayer">Whether this action targets the player (<c>true</c>) or the enemy (<c>false</c>).</param>
			/// <returns>The new temp hull loss action.</returns>
			ILossAction MakeLossAction(int amount, bool targetPlayer = true);
			
			/// <summary>
			/// Represents a temp hull gain action.
			/// </summary>
			public interface IGainAction : ICardAction<CardAction>
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
				/// Sets <see cref="TargetPlayer"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IGainAction SetTargetPlayer(bool value);

				/// <summary>
				/// Sets <see cref="Amount"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IGainAction SetAmount(int value);
			}
			
			/// <summary>
			/// Represents a temp hull loss action.
			/// </summary>
			public interface ILossAction : ICardAction<CardAction>
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
				ILossAction SetTargetPlayer(bool value);

				/// <summary>
				/// Sets <see cref="Amount"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ILossAction SetAmount(int value);

				/// <summary>
				/// Sets <see cref="CannotKill"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ILossAction SetCannotKill(bool value);
			}
		}
	}
}