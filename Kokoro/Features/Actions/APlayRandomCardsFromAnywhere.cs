using Shockah.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Kokoro;

public sealed class APlayRandomCardsFromAnywhere : CardAction
{
	public HashSet<int> CardIds = [];
	public int Amount = 1;
	public bool ShowTheCardIfNotInHand = true;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		var potentialCards = ModEntry.Instance.Api.GetCardsEverywhere(s)
			.Where(c => CardIds.Contains(c.uuid))
			.Where(c => !c.GetDataWithOverrides(s).unplayable);

		foreach (var card in potentialCards.Shuffle(s.rngActions).Take(Amount))
			c.QueueImmediate(new APlaySpecificCardFromAnywhere { CardId = card.uuid, ShowTheCardIfNotInHand = ShowTheCardIfNotInHand });
	}
}
