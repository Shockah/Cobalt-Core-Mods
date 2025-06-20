namespace Shockah.Dracula;

internal sealed class SpacerTooltip : Tooltip
{
	public int Height;
	
	public override Rect Render(G g, bool dontDraw)
	{
		var box = g.Push();
		g.Pop();
		return new(box.rect.x, box.rect.y, 0, Height);
	}
}