using System;

namespace Shockah.Rerolls;

internal sealed class ADelegateAction : CardAction
{
	private Action<G, State, Combat> Delegate;

	public ADelegateAction(Action<G, State, Combat> @delegate)
	{
		this.Delegate = @delegate;
	}

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		Delegate(g, s, c);
	}
}
