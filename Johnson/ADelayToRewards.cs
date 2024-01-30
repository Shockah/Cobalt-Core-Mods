namespace Shockah.Johnson;

public sealed class ADelayToRewards : CardAction
{
	public required CardAction Action;

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
		s.rewardsQueue.QueueImmediate(Action);
	}
}
