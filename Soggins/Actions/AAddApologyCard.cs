using System.Linq;

namespace Shockah.Soggins;

public sealed class AAddApologyCard : CardAction
{
	public int Amount = 1;
	public CardDestination Destination = CardDestination.Hand;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);

		var cards = Enumerable.Range(0, Amount)
			.Select(_ => SmugStatusManager.GenerateAndTrackApology(s, c, s.rngActions))
			.Reverse();

		foreach (var card in cards)
			c.QueueImmediate(new AAddCard
			{
				card = card,
				destination = Destination
			});
	}
}
