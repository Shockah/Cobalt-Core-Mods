using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IHeavyApi"/>
		IHeavyApi Heavy { get; }

		/// <summary>
		/// Allows working with and using the Heavy card trait.
		/// Heavy cards do not discard or exhaust on the turn they are drawn.
		/// </summary>
		public interface IHeavyApi
		{
			/// <summary>
			/// The Heavy card trait.
			/// </summary>
			ICardTraitEntry Trait { get; }

			/// <summary>
			/// Returns whether the Heavy card is already used up (and will discard/exhaust as usual).
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The Heavy card.</param>
			/// <returns>Whether the Heavy card is already used up (and will discard/exhaust as usual).</returns>
			bool IsHeavyUsed(State state, Card card);

			/// <summary>
			/// Sets whether the Heavy card is already used up (and will discard/exhaust as usual).
			/// </summary>
			/// <param name="state">The game state.</param>
			/// <param name="card">The Heavy card.</param>
			/// <param name="value">Whether the Heavy card is already used up (and will discard/exhaust as usual).</param>
			void SetHeavyUsed(State state, Card card, bool value);

			/// <summary>
			/// Allows modifying an <see cref="ACardSelect"/> action with Heavy-related changes.
			/// </summary>
			/// <param name="action">The action to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardSelect ModifyCardSelect(ACardSelect action);
			
			/// <summary>
			/// Allows modifying a <see cref="CardBrowse"/> route with Heavy-related changes.
			/// </summary>
			/// <param name="route">The route to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardBrowse ModifyCardBrowse(CardBrowse route);
			
			/// <summary>
			/// An <see cref="ACardSelect"/> action wrapper, which allows modifying it with Heavy-related changes.
			/// </summary>
			public interface ICardSelect : ICardAction<ACardSelect>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Heavy trait.
				/// </summary>
				bool? FilterHeavy { get; set; }

				/// <summary>
				/// Sets <see cref="FilterHeavy"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardSelect SetFilterHeavy(bool? value);
			}
			
			/// <summary>
			/// A <see cref="CardBrowse"/> route wrapper, which allows modifying it with Heavy-related changes.
			/// </summary>
			public interface ICardBrowse : IRoute<CardBrowse>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Heavy trait.
				/// </summary>
				bool? FilterHeavy { get; set; }

				/// <summary>
				/// Sets <see cref="FilterHeavy"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardBrowse SetFilterHeavy(bool? value);
			}
		}
	}
}
