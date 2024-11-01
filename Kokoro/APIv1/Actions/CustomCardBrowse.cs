using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		ACardSelect MakeCustomCardBrowse(ACardSelect action, ICustomCardBrowseSource source);
		CardBrowse MakeCustomCardBrowse(CardBrowse route, ICustomCardBrowseSource source);
	}
}

public interface ICustomCardBrowseSource
{
	IEnumerable<Tooltip> GetSearchTooltips(State state);
	string GetTitle(State state, Combat? combat, List<Card> cards);
	List<Card> GetCards(State state, Combat? combat);
}
