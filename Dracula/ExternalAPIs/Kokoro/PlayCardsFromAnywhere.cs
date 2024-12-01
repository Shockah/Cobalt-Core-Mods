using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		/// <inheritdoc cref="IPlayCardsFromAnywhereApi"/>
		IPlayCardsFromAnywhereApi PlayCardsFromAnywhere { get; }

		/// <summary>
		/// Allows using and creating actions, which are able to play cards, wherever they are.
		/// </summary>
		public interface IPlayCardsFromAnywhereApi
		{
			/// <summary>
			/// Casts the action to <see cref="IPlayCardsFromAnywhereAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IPlayCardsFromAnywhereAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IPlayCardsFromAnywhereAction? AsAction(CardAction action);
			
			/// <summary>
			/// Creates a new action that will play a card with the given ID.
			/// </summary>
			/// <param name="cardId">The ID of the card.</param>
			/// <returns>A new action that will play a card with the given ID.</returns>
			IPlayCardsFromAnywhereAction MakeAction(int cardId);
			
			/// <summary>
			/// Creates a new action that will play the given card.
			/// </summary>
			/// <param name="card">The card.</param>
			/// <returns>A new action that will play the given card.</returns>
			IPlayCardsFromAnywhereAction MakeAction(Card card);
			
			/// <summary>
			/// Creates a new action that will play one or more cards with the given IDs.
			/// </summary>
			/// <param name="cardIds">The card IDs.</param>
			/// <param name="amount">The total amount of cards to play.</param>
			/// <returns>A new action that will play one or more cards with the given IDs.</returns>
			IPlayCardsFromAnywhereAction MakeAction(IEnumerable<int> cardIds, int amount = 1);
			
			/// <summary>
			/// Creates a new action that will play one or more of the given cards.
			/// </summary>
			/// <param name="cards">The cards.</param>
			/// <param name="amount">The total amount of cards to play.</param>
			/// <returns>A new action that will play one or more of the given cards.</returns>
			IPlayCardsFromAnywhereAction MakeAction(IEnumerable<Card> cards, int amount = 1);
			
			/// <summary>
			/// Creates a new action that will play one or more of the given cards.
			/// </summary>
			/// <param name="cards">The card IDs, along with their fallback cards. See <see cref="IPlayCardsFromAnywhereAction.Cards"/>.</param>
			/// <param name="amount">The total amount of cards to play.</param>
			/// <returns>A new action that will play one or more of the given cards.</returns>
			IPlayCardsFromAnywhereAction MakeAction(IEnumerable<(int CardId, Card? FallbackCard)> cards, int amount = 1);
			
			public interface IPlayCardsFromAnywhereAction : ICardAction<CardAction>
			{
				/// <summary>
				/// A list of card IDs and their fallback cards that could be played.
				/// </summary>
				/// <para>
				/// If a card could not be found by its ID, the fallback card is played instead.
				/// </para>
				IList<(int CardId, Card? FallbackCard)> Cards { get; set; }
				
				/// <summary>
				/// The total amount of cards to play.
				/// </summary>
				int Amount { get; set; }
				
				/// <summary>
				/// Whether the card should first be visually moved to hand. Defaults to <c>true</c>.
				/// </summary>
				bool ShowTheCardIfNotInHand { get; set; }

				/// <summary>
				/// Sets <see cref="Cards"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IPlayCardsFromAnywhereAction SetCardIds(IEnumerable<int> value);
				
				/// <summary>
				/// Sets <see cref="Cards"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IPlayCardsFromAnywhereAction SetCards(IEnumerable<Card> value);
				
				/// <summary>
				/// Sets <see cref="Cards"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IPlayCardsFromAnywhereAction SetCards(IEnumerable<(int CardId, Card? FallbackCard)> value);
				
				/// <summary>
				/// Sets <see cref="Amount"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IPlayCardsFromAnywhereAction SetAmount(int value);
				
				/// <summary>
				/// Sets <see cref="ShowTheCardIfNotInHand"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IPlayCardsFromAnywhereAction SetShowTheCardIfNotInHand(bool value);
			}
		}
	}
}
