using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		IPlayCardsFromAnywhereApi PlayCardsFromAnywhere { get; }

		public interface IPlayCardsFromAnywhereApi
		{
			IPlayCardsFromAnywhereAction? AsAction(CardAction action);
			IPlayCardsFromAnywhereAction MakeAction(int cardId);
			IPlayCardsFromAnywhereAction MakeAction(IEnumerable<int> cardIds, int amount = 1);
			
			public interface IPlayCardsFromAnywhereAction : ICardAction<CardAction>
			{
				HashSet<int> CardIds { get; set; }
				int Amount { get; set; }
				bool ShowTheCardIfNotInHand { get; set; }

				IPlayCardsFromAnywhereAction SetCardIds(HashSet<int> value);
				IPlayCardsFromAnywhereAction SetAmount(int value);
				IPlayCardsFromAnywhereAction SetShowTheCardIfNotInHand(bool value);
			}
		}
	}
}
