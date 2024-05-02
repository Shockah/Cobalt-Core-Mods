namespace Shockah.Soggins;

public sealed class AShakeShip : CardAction
{
	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		s.ship.shake += 1;
	}
}
