using System;

namespace Shockah.Shared;

internal sealed class MouseDownHandler(Action @delegate) : OnMouseDown
{
	public void OnMouseDown(G g, Box b)
		=> @delegate();
}