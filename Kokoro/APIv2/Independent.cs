using Nickel;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IIndependentApi"/>
		IIndependentApi Independent { get; }

		/// <summary>
		/// Allows working with and using the Independent card trait.
		/// Independent cards ignore the Is Missing status of their owner - actions play as normal, and the status does not decrease.
		/// </summary>
		public interface IIndependentApi
		{
			/// <summary>
			/// The Fleeting card trait.
			/// </summary>
			ICardTraitEntry Trait { get; }

			/// <summary>
			/// Allows modifying an <see cref="ACardSelect"/> action with Independent-related changes.
			/// </summary>
			/// <param name="action">The action to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardSelect ModifyCardSelect(ACardSelect action);
			
			/// <summary>
			/// Allows modifying a <see cref="CardBrowse"/> route with Independent-related changes.
			/// </summary>
			/// <param name="route">The route to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardBrowse ModifyCardBrowse(CardBrowse route);
			
			/// <summary>
			/// An <see cref="ACardSelect"/> action wrapper, which allows modifying it with Independent-related changes.
			/// </summary>
			public interface ICardSelect : ICardAction<ACardSelect>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Independent trait.
				/// </summary>
				bool? FilterIndependent { get; set; }

				/// <summary>
				/// Sets <see cref="FilterIndependent"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardSelect SetFilterIndependent(bool? value);
			}
			
			/// <summary>
			/// A <see cref="CardBrowse"/> route wrapper, which allows modifying it with Independent-related changes.
			/// </summary>
			public interface ICardBrowse : IRoute<CardBrowse>
			{
				/// <summary>
				/// When set, will only show cards which also have (or not) the Independent trait.
				/// </summary>
				bool? FilterIndependent { get; set; }

				/// <summary>
				/// Sets <see cref="FilterIndependent"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardBrowse SetFilterIndependent(bool? value);
			}
		}
	}
}
