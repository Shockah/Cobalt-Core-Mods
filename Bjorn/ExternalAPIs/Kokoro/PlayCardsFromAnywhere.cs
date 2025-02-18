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
			
			/// <summary>
			/// Casts the action to <see cref="IModifyCardAnywhereAction"/>, if it is one.
			/// </summary>
			/// <param name="action">The action.</param>
			/// <returns>The <see cref="IModifyCardAnywhereAction"/>, if the given action is one, or <c>null</c> otherwise.</returns>
			IModifyCardAnywhereAction? AsModifyAction(CardAction action);

			/// <summary>
			/// Creates a new action that will show and modify a card, wherever it is.
			/// </summary>
			/// <param name="cardId">The ID of the card to modify.</param>
			/// <param name="action">The modification action. Its <see cref="CardAction.selectedCard"/> will be set to the card to modify.</param>
			/// <returns></returns>
			IModifyCardAnywhereAction MakeModifyAction(int cardId, CardAction action);
			
			/// <summary>
			/// Represents an action that will play some cards, wherever they are.
			/// </summary>
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

			/// <summary>
			/// Represents an action that will show and modify a card, wherever it is.
			/// </summary>
			public interface IModifyCardAnywhereAction : ICardAction<CardAction>
			{
				/// <summary>
				/// The ID of the card to modify.
				/// </summary>
				int CardId { get; set; }
				
				/// <summary>
				/// The actual action that modifies the card.
				/// </summary>
				CardAction Action { get; set; }
				
				/// <summary>
				/// Whether the card should still be moved to the center of the screen if it's in hand.
				/// </summary>
				bool MoveIfInHand { get; set; }
				
				/// <summary>
				/// The amount of extra time the card should stay presented after the whole modification.
				/// </summary>
				double PrePauseTime { get; set; }
				
				/// <summary>
				/// The amount of extra time the card should stay presented after the whole modification.
				/// </summary>
				double PostPauseTime { get; set; }
				
				/// <summary>
				/// Whether the animation should flip the card visually when the actual modification begins.
				/// </summary>
				bool FlipOnBegin { get; set; }
				
				/// <summary>
				/// Whether the animation should flip the card visually when the actual modification ends.
				/// </summary>
				bool FlipOnEnd { get; set; }
				
				/// <summary>
				/// Whether the animation should flop/toggle the card visually when the actual modification begins.
				/// </summary>
				bool FlopOnBegin { get; set; }
				
				/// <summary>
				/// Whether the animation should flop/toggle the card visually when the actual modification ends.
				/// </summary>
				bool FlopOnEnd { get; set; }
				
				/// <summary>
				/// Sets <see cref="CardId"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IModifyCardAnywhereAction SetCardId(int value);
				
				/// <summary>
				/// Sets <see cref="Action"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IModifyCardAnywhereAction SetAction(CardAction value);
				
				/// <summary>
				/// Sets <see cref="MoveIfInHand"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IModifyCardAnywhereAction SetMoveIfInHand(bool value);
				
				/// <summary>
				/// Sets <see cref="PrePauseTime"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IModifyCardAnywhereAction SetPrePauseTime(double value);
				
				/// <summary>
				/// Sets <see cref="PostPauseTime"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IModifyCardAnywhereAction SetPostPauseTime(double value);
				
				/// <summary>
				/// Sets <see cref="FlipOnBegin"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IModifyCardAnywhereAction SetFlipOnBegin(bool value);
				
				/// <summary>
				/// Sets <see cref="FlipOnEnd"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IModifyCardAnywhereAction SetFlipOnEnd(bool value);
				
				/// <summary>
				/// Sets <see cref="FlopOnBegin"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IModifyCardAnywhereAction SetFlopOnBegin(bool value);
				
				/// <summary>
				/// Sets <see cref="FlopOnEnd"/>.
				/// </summary>
				/// <param name="value">The new value.</param>
				/// <returns>This object after the change.</returns>
				IModifyCardAnywhereAction SetFlopOnEnd(bool value);
			}
		}
	}
}
