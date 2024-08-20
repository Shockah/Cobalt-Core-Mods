using System;

namespace Shockah.Shared;

internal sealed class InputPhaseHandler(Action @delegate) : OnInputPhase
{
	public void OnInputPhase(G g, Box b)
		=> @delegate();
}