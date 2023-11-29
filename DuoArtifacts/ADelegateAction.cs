using System;

namespace Shockah.DuoArtifacts;

internal sealed class ADelegateAction : CardAction
{
	private Action Delegate;

	public ADelegateAction(Action @delegate)
	{
		this.Delegate = @delegate;
	}

	public override void Begin(G g, State s, Combat c)
	{
		base.Begin(g, s, c);
		Delegate();
	}
}
