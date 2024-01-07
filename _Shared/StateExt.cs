using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

internal static class StateExt
{
#if !IS_NICKEL_MOD
	public static State? Instance
		=> GExt.Instance?.state;
#endif

	public static IEnumerable<Card> GetAllCards(this State state)
	{
		IEnumerable<Card> results = state.deck;
		if (state.route is Combat combat)
			results = results.Concat(combat.hand).Concat(combat.discard).Concat(combat.exhausted);
		return results;
	}
}