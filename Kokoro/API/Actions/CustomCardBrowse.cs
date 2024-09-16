using System.Collections.Generic;

namespace Shockah.Kokoro;

public partial interface IKokoroApi
{
	public partial interface IActionApi
	{
		ACardSelect MakeCustomCardBrowse(ACardSelect action, ICustomCardBrowseSource source);
	}
}

public interface ICustomCardBrowseSource
{
	List<Card> GetCards(State state, Combat? combat);
	string GetTitle(State state, Combat? combat, List<Card> cards);
}
