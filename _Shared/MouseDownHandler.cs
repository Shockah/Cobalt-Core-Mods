using System;

namespace Shockah.Shared;

internal sealed class MouseDownHandler : OnMouseDown
{
	private readonly Action Delegate;

	public MouseDownHandler(Action @delegate)
	{
		this.Delegate = @delegate;
	}

	public void OnMouseDown(G g, Box b)
		=> Delegate();
}