namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="ICardDestinationApi"/>
		ICardDestinationApi CardDestination { get; }

		/// <summary>
		/// Allows changing the card destination of a card offering/reward action/screen.
		/// </summary>
		public interface ICardDestinationApi
		{
			/// <summary>
			/// Allows modifying an <see cref="ACardOffering"/> action with card destination changes.
			/// </summary>
			/// <param name="action">The action to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardOffering ModifyCardOffering(ACardOffering action);
			
			/// <summary>
			/// Allows modifying a <see cref="CardReward"/> route with card destination changes.
			/// </summary>
			/// <param name="route">The route to modify.</param>
			/// <returns>A wrapper, granting access to the modifications.</returns>
			ICardReward ModifyCardReward(CardReward route);
			
			/// <summary>
			/// An <see cref="ACardOffering"/> action wrapper, which allows modifying it with card destination changes.
			/// </summary>
			public interface ICardOffering : ICardAction<ACardOffering>
			{
				/// <summary>
				/// A destination override for where the chosen card should go to.
				/// </summary>
				CardDestination? Destination { get; set; }
				
				/// <summary>
				/// Whether the card should be inserted randomly into the destination.
				/// Only affects the <see cref="CardDestination.Deck"/> and <see cref="CardDestination.Hand"/> destinations.
				/// If left <c>null</c>, it will depend on the destination (randomly for <see cref="CardDestination.Deck"/>, at the end for <see cref="CardDestination.Hand"/>).
				/// </summary>
				bool? InsertRandomly { get; set; }

				/// <summary>
				/// Sets <see cref="Destination"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardOffering SetDestination(CardDestination? value);
				
				/// <summary>
				/// Sets <see cref="InsertRandomly"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardOffering SetInsertRandomly(bool? value);
			}
			
			/// <summary>
			/// A <see cref="CardReward"/> route wrapper, which allows modifying it with card destination changes.
			/// </summary>
			public interface ICardReward : IRoute<CardReward>
			{
				/// <summary>
				/// A destination override for where the chosen card should go to.
				/// </summary>
				CardDestination? Destination { get; set; }
				
				/// <summary>
				/// Whether the card should be inserted randomly into the destination.
				/// Only affects the <see cref="CardDestination.Deck"/> and <see cref="CardDestination.Hand"/> destinations.
				/// If left <c>null</c>, it will depend on the destination (randomly for <see cref="CardDestination.Deck"/>, at the end for <see cref="CardDestination.Hand"/>).
				/// </summary>
				bool? InsertRandomly { get; set; }

				/// <summary>
				/// Sets <see cref="Destination"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardReward SetDestination(CardDestination? value);
				
				/// <summary>
				/// Sets <see cref="InsertRandomly"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				ICardReward SetInsertRandomly(bool? value);
			}
		}
	}
}
