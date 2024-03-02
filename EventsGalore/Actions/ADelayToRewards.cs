using System.Collections.Generic;

namespace Shockah.EventsGalore;

public sealed class ADelayToRewards : CardAction
{
	public required List<CardAction> Actions;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
		s.rewardsQueue.InsertRange(0, Actions);
	}
}
