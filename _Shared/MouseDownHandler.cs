using System;

namespace Shockah.Shared;

internal sealed class MouseDownHandler(Action @delegate) : OnMouseDown
{
	private readonly Action Delegate = @delegate;

	public void OnMouseDown(G g, Box b)
		=> Delegate();
}