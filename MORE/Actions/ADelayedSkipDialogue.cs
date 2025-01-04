namespace Shockah.MORE;

internal sealed class ADelayedSkipDialogue : CardAction
{
	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;
		s.GetCurrentQueue().Queue(new ASkipDialogue());
	}
}