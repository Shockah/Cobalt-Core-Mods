using System;

namespace Shockah.Shared;

internal sealed class InputPhaseHandler(Action @delegate) : OnInputPhase
{
	private readonly Action Delegate = @delegate;

	public void OnInputPhase(G g, Box b)
		=> Delegate();
}