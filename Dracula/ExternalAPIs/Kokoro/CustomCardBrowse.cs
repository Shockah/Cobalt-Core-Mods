using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IV2
	{
		ICustomCardBrowseApi CustomCardBrowse { get; }
		
		public interface ICustomCardBrowseApi
		{
			public interface ICustomCardBrowseSource
			{
				IEnumerable<Tooltip> GetSearchTooltips(State state);
				string GetTitle(State state, Combat? combat, List<Card> cards);
				List<Card> GetCards(State state, Combat? combat);
			}

			public interface IAction : ICardAction<ACardSelect>
			{
				ICustomCardBrowseSource? CustomBrowseSource { get; set; }

				IAction SetCustomBrowseSource(ICustomCardBrowseSource? source);
			}
			
			public interface IRoute : IRoute<CardBrowse>
			{
				ICustomCardBrowseSource? CustomBrowseSource { get; set; }

				IRoute SetCustomBrowseSource(ICustomCardBrowseSource? source);
			}

			IAction ModifyCardSelect(ACardSelect action);
			IRoute ModifyCardBrowse(CardBrowse route);
		}
	}
}
