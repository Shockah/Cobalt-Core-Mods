namespace Shockah.MORE;

internal sealed class ASkipDialogue : CardAction
{
	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		timer = 0;

		for (var i = 0; i < 100; i++)
			s.GetDialogue()?.Advance(g);
	}
}