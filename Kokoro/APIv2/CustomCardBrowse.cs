using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ICustomCardBrowseApi CustomCardBrowse { get; }
		
		public interface ICustomCardBrowseApi
		{
			ICardSelect ModifyCardSelect(ACardSelect action);
			ICardBrowse ModifyCardBrowse(CardBrowse route);

			public interface ICardSelect : ICardAction<ACardSelect>
			{
				ICustomCardBrowseSource? CustomBrowseSource { get; set; }

				ICardSelect SetCustomBrowseSource(ICustomCardBrowseSource? source);
			}
			
			public interface ICardBrowse : IRoute<CardBrowse>
			{
				ICustomCardBrowseSource? CustomBrowseSource { get; set; }

				ICardBrowse SetCustomBrowseSource(ICustomCardBrowseSource? source);
			}
			
			public interface ICustomCardBrowseSource
			{
				IEnumerable<Tooltip> GetSearchTooltips(State state);
				string GetTitle(State state, Combat? combat, List<Card> cards);
				List<Card> GetCards(State state, Combat? combat);
			}
		}
	}
}
