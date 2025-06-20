using System.Collections.Generic;

namespace Shockah.Dracula;

internal sealed class ActionTooltip : Tooltip
{
	public List<CardAction> Actions = [];
	public int ActionSpacing = 4;
	
	public override Rect Render(G g, bool dontDraw)
	{
		var box = g.Push();
		
		var totalActionWidth = 0;
		foreach (var action in Actions)
		{
			if (totalActionWidth != 0)
				totalActionWidth += ActionSpacing;
			
			g.Push(rect: new(totalActionWidth, 4));
			totalActionWidth += Card.RenderAction(g, g.state, action, dontDraw: dontDraw);
			g.Pop();
		}

		g.Pop();
		return new(box.rect.x, box.rect.y, totalActionWidth, 9);
	}
}